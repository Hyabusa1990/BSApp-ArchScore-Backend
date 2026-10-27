using Fawkes.Api.Authentication;
using Fawkes.Core.Abstractions;
using Fawkes.Core.Model;
using Fawkes.Core.Rules;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Fawkes.Api
{
    /// <summary>
    /// GET /displays/data through the real pipeline and the real DisplayService; only the data store is replaced.
    /// </summary>
    [TestFixture]
    public class DisplaysTests
    {
        private const string DeviceCode = "123456";
        private const int FixtureId = 3;

        private WebApplicationFactory<Program> factory;
        private HttpClient client;
        private IFawkesDataStore dataStore;

        [SetUp]
        public void Setup()
        {
            dataStore = Substitute.For<IFawkesDataStore>();

            factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.UseSetting("Jwt:Key", "integration-test-user-key-0123456789-0123456789-0123456789");
                builder.UseSetting("JwtDevice:Key", "integration-test-device-key-0123456789-0123456789-012345");
                builder.ConfigureTestServices(services => services.AddScoped(_ => dataStore));
            });

            client = factory.CreateClient();
            var token = factory.Services.GetRequiredService<ITokenService>().GenerateAccessTokenForDevice(DeviceCode);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        [TearDown]
        public void TearDown()
        {
            client.Dispose();
            factory.Dispose();
        }

        private void SetupDevice(DisplayType displayType, int currentRoundNo)
        {
            dataStore.GetDeviceByCodeAsync(DeviceCode).Returns(new Device()
            {
                Code = DeviceCode,
                FixtureId = FixtureId,
                DisplayType = displayType,
                DisplayTheme = DisplayTheme.Dark
            });
            dataStore.GetFixtureAsync(FixtureId).Returns(new Fixture() { Id = FixtureId, CurrentRoundNo = currentRoundNo, RuleSetKey = RuleSetKeys.Default });
        }

        private async Task<JsonElement> GetDisplayDataAsync()
        {
            var response = await client.GetAsync("/displays/data");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        [Test]
        public async Task MatchDisplayWithoutCurrentRoundShowsNothing()
        {
            SetupDevice(DisplayType.Match, currentRoundNo: 0);
            // There are no target assignments for round 0.
            dataStore.GetTargetDataAsync(FixtureId, (int?)0, (int?)1).Returns(Array.Empty<TargetData>());

            var data = await GetDisplayDataAsync();

            Assert.That(data.GetProperty("displayType").GetString(), Is.EqualTo("None"));
            Assert.That(data.GetProperty("displayTheme").GetString(), Is.EqualTo("Dark"));
        }

        [Test]
        public async Task TableDisplayWithoutCurrentRoundShowsInitialTable()
        {
            SetupDevice(DisplayType.Table, currentRoundNo: 0);
            dataStore.GetLeagueTableAsync(FixtureId).Returns(new LeagueTable()
            {
                Positions = [new LeagueTablePosition() { TeamId = 1, TeamName = "A", Rank = 1 }, new LeagueTablePosition() { TeamId = 2, TeamName = "B", Rank = 2 }]
            });

            var data = await GetDisplayDataAsync();

            Assert.That(data.GetProperty("displayType").GetString(), Is.EqualTo("Table"));
            Assert.That(data.GetProperty("leagueTablePositions").GetArrayLength(), Is.EqualTo(2));
        }

        [Test]
        public async Task NoneDisplayWithoutCurrentRoundShowsNothing()
        {
            SetupDevice(DisplayType.None, currentRoundNo: 0);

            var data = await GetDisplayDataAsync();

            Assert.That(data.GetProperty("displayType").GetString(), Is.EqualTo("None"));
        }
    }
}
