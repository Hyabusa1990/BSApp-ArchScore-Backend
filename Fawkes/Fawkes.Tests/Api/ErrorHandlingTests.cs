using Fawkes.Api.Authentication;
using Fawkes.Core.Exceptions;
using Fawkes.Core.Model;
using Fawkes.Core.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Fawkes.Api
{
    /// <summary>
    /// Runs requests through the real ASP.NET Core pipeline (routing, JWT authentication, controllers,
    /// exception handler) with the Core services replaced by substitutes.
    /// </summary>
    [TestFixture]
    public class ErrorHandlingTests
    {
        private const string User = "owner@example.org";
        private static readonly Guid FixtureUniqueId = Guid.Parse("6f1c2b7e-3d4a-4c5b-9e8f-0a1b2c3d4e5f");

        private WebApplicationFactory<Program> factory;
        private HttpClient client;

        private IFixtureService fixtureService;
        private IDeviceService deviceService;
        private IDosService dosService;
        private ISpotterService spotterService;
        private IMatchPlayChartService matchPlayChartService;

        [SetUp]
        public void Setup()
        {
            fixtureService = Substitute.For<IFixtureService>();
            deviceService = Substitute.For<IDeviceService>();
            dosService = Substitute.For<IDosService>();
            spotterService = Substitute.For<ISpotterService>();
            matchPlayChartService = Substitute.For<IMatchPlayChartService>();

            factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.UseSetting("Jwt:Key", "integration-test-user-key-0123456789-0123456789-0123456789");
                builder.UseSetting("JwtDevice:Key", "integration-test-device-key-0123456789-0123456789-012345");
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(fixtureService);
                    services.AddSingleton(deviceService);
                    services.AddSingleton(dosService);
                    services.AddSingleton(spotterService);
                    services.AddSingleton(matchPlayChartService);
                });
            });

            client = factory.CreateClient();
        }

        [TearDown]
        public void TearDown()
        {
            client.Dispose();
            factory.Dispose();
        }

        private void Authenticate()
        {
            var token = factory.Services.GetRequiredService<ITokenService>()
                .GenerateAccessToken(new IdentityUser() { Id = Guid.NewGuid().ToString(), Email = User });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response)
        {
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private static readonly object FixtureRequest = new
        {
            date = "2026-10-10",
            location = "Kassel",
            leagueName = "Hessenliga",
            fixtureName = "1. Wettkampftag"
        };

        // --- Mapping of domain exceptions --------------------------------------------------------

        [Test]
        public async Task MissingTokenIs401()
        {
            var response = await client.GetAsync("/fixtures/1");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task AccessDeniedIs403WithProblemDetails()
        {
            Authenticate();
            fixtureService.GetFixtureAsync(1, User).ThrowsAsync(new AccessDeniedException("No access to fixture 1."));

            var response = await client.GetAsync("/fixtures/1");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            var problem = await ReadProblemAsync(response);
            Assert.That(problem.GetProperty("status").GetInt32(), Is.EqualTo(403));
            Assert.That(problem.GetProperty("code").GetString(), Is.EqualTo("ACCESS_DENIED"));
            Assert.That(problem.GetProperty("detail").GetString(), Is.EqualTo("No access to fixture 1."));
        }

        [Test]
        public async Task NotFoundIs404WithCode()
        {
            Authenticate();
            fixtureService.GetFixtureAsync(1, User).ThrowsAsync(new NotFoundException("Fixture 1 not found.", "FIXTURE_NOT_FOUND"));

            var response = await client.GetAsync("/fixtures/1");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That((await ReadProblemAsync(response)).GetProperty("code").GetString(), Is.EqualTo("FIXTURE_NOT_FOUND"));
        }

        [Test]
        public async Task ConflictIs409()
        {
            spotterService.ConfirmEndScoreAsync(FixtureUniqueId, 3).ThrowsAsync(new ConflictException("Already confirmed.", "SET_ALREADY_CONFIRMED"));

            var response = await client.PutAsync($"/fixtures/{FixtureUniqueId}/targets/3/spotter/shots/confirm", null);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That((await ReadProblemAsync(response)).GetProperty("code").GetString(), Is.EqualTo("SET_ALREADY_CONFIRMED"));
        }

        [Test]
        public async Task InvalidRequestIs400()
        {
            Authenticate();
            dosService.SetConfirmedSetScore(1, 1, 2, 9, 54, User).ThrowsAsync(new InvalidRequestException("Set number must be between 1 and 5.", "INVALID_SET_NO"));

            var response = await client.PutAsJsonAsync("/fixtures/1/rounds/1/targets/2/sets/9/score", new { score = 54 });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That((await ReadProblemAsync(response)).GetProperty("code").GetString(), Is.EqualTo("INVALID_SET_NO"));
        }

        [Test]
        public async Task UnexpectedErrorIs500WithoutInternalDetails()
        {
            Authenticate();
            fixtureService.GetFixtureAsync(1, User).ThrowsAsync(new Exception("connection string with secret"));

            var response = await client.GetAsync("/fixtures/1");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
            await ReadProblemAsync(response);
            Assert.That(await response.Content.ReadAsStringAsync(), Does.Not.Contain("secret"));
        }

        // --- Endpoints that used to answer with 500 ----------------------------------------------

        [Test]
        public async Task GetPhaseWithoutAccessIs403()
        {
            Authenticate();
            fixtureService.GetFixtureAsync(1, User).ThrowsAsync(new AccessDeniedException("No access."));

            var response = await client.GetAsync("/fixtures/1/phase");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        [Test]
        public async Task SetPhaseWithoutWriteAccessIs403()
        {
            Authenticate();
            fixtureService.SetPhaseAsync(1, 2, User).ThrowsAsync(new AccessDeniedException("No write access."));

            var response = await client.PutAsJsonAsync("/fixtures/1/phase", new { roundNo = 2 });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        [Test]
        public async Task GetRoundOfMissingFixtureIs404()
        {
            Authenticate();
            dosService.GetRoundMatchesAsync(1, 1, User).ThrowsAsync(new NotFoundException("Fixture 1 not found."));

            var response = await client.GetAsync("/fixtures/1/rounds/1");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task UnknownDisplayTypeNumberIs400()
        {
            Authenticate();

            var response = await client.PutAsJsonAsync("/fixtures/1/devices/7", new { displayType = 99, displayTheme = "Light" });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That((await ReadProblemAsync(response)).GetProperty("code").GetString(), Is.EqualTo("INVALID_DISPLAY_TYPE"));
            await deviceService.DidNotReceiveWithAnyArgs().UpdateDeviceAsync(default, default, default!, default, default, default);
        }

        // --- Success codes -----------------------------------------------------------------------

        [Test]
        public async Task CreateFixtureIs201WithLocation()
        {
            Authenticate();
            fixtureService.CreateFixtureAsync(Arg.Any<DateTime>(), "Kassel", "Hessenliga", "1. Wettkampftag", User)
                .Returns(new Fixture() { Id = 42, FixtureName = "1. Wettkampftag" });

            var response = await client.PostAsJsonAsync("/fixtures", FixtureRequest);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(response.Headers.Location?.ToString(), Is.EqualTo("/fixtures/42"));
            Assert.That((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32(), Is.EqualTo(42));
        }

        [Test]
        public async Task SetPhaseIs204()
        {
            Authenticate();

            var response = await client.PutAsJsonAsync("/fixtures/1/phase", new { roundNo = 2 });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            await fixtureService.Received(1).SetPhaseAsync(1, 2, User);
        }

        [Test]
        public async Task UpdateSetScoreIs204()
        {
            Authenticate();

            var response = await client.PutAsJsonAsync("/fixtures/1/rounds/1/targets/2/sets/3/score", new { score = 54 });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            await dosService.Received(1).SetConfirmedSetScore(1, 1, 2, 3, 54, User);
        }

        [Test]
        public async Task UnassignDeviceIs204()
        {
            Authenticate();

            var response = await client.PutAsync("/fixtures/1/devices/7/unassign", null);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            await deviceService.Received(1).RemoveDeviceFromFixtureAsync(1, 7, User);
        }

        [Test]
        public async Task CreateMatchPlayChartIs201()
        {
            Authenticate();

            var response = await client.PostAsJsonAsync("/fixtures/1/matchplaychart", new { teams = new[] { new { name = "A" }, new { name = "B" } } });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        }
    }
}
