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

        [Test]
        public async Task OverviewShowsTableAndAllMatchesOfCurrentRound()
        {
            SetupDevice(DisplayType.Overview, currentRoundNo: 1);
            dataStore.GetLeagueTableAsync(FixtureId).Returns(new LeagueTable()
            {
                Positions = [new LeagueTablePosition() { TeamId = 1, TeamName = "A", Rank = 1 }, new LeagueTablePosition() { TeamId = 2, TeamName = "B", Rank = 2 }]
            });
            // Match 2 is listed first on purpose: the overview orders matches by number.
            dataStore.GetTargetDataAsync(FixtureId, (int?)1, null).Returns(new[]
            {
                new TargetData() { RoundNo = 1, TargetNo = 3, TeamName = "C", Shots = "999" },
                new TargetData() { RoundNo = 1, TargetNo = 4, TeamName = "D", Shots = "888" },
                new TargetData() { RoundNo = 1, TargetNo = 1, TeamName = "A", Shots = "++++++", ConfirmedSet01Score = 60 },
                new TargetData() { RoundNo = 1, TargetNo = 2, TeamName = "B", Shots = "999999", ConfirmedSet01Score = 54 }
            });

            var data = await GetDisplayDataAsync();

            Assert.That(data.GetProperty("displayType").GetString(), Is.EqualTo("Overview"));
            Assert.That(data.GetProperty("leagueTablePositions").GetArrayLength(), Is.EqualTo(2));

            var matches = data.GetProperty("matches").EnumerateArray().ToList();
            Assert.That(matches.Select(m => m.GetProperty("matchNo").GetInt32()), Is.EqualTo(new[] { 1, 2 }));

            var match1 = matches[0].GetProperty("targets").EnumerateArray().ToList();
            Assert.That(match1.Select(t => t.GetProperty("teamName").GetString()), Is.EqualTo(new[] { "A", "B" }));
            Assert.That(match1.Select(t => t.GetProperty("setPoints").GetInt32()), Is.EqualTo(new[] { 2, 0 }));

            var match2 = matches[1].GetProperty("targets").EnumerateArray().ToList();
            Assert.That(match2.Select(t => t.GetProperty("shots").GetString()), Is.EqualTo(new[] { "999", "888" }));
            Assert.That(match2.Select(t => t.GetProperty("currentSetScore").GetInt32()), Is.EqualTo(new[] { 27, 24 }));
        }

        [Test]
        public async Task OverviewWithoutCurrentRoundShowsOnlyTheTable()
        {
            SetupDevice(DisplayType.Overview, currentRoundNo: 0);
            dataStore.GetLeagueTableAsync(FixtureId).Returns(new LeagueTable()
            {
                Positions = [new LeagueTablePosition() { TeamId = 1, TeamName = "A", Rank = 1 }]
            });
            dataStore.GetTargetDataAsync(FixtureId, (int?)0, null).Returns(Array.Empty<TargetData>());

            var data = await GetDisplayDataAsync();

            Assert.That(data.GetProperty("displayType").GetString(), Is.EqualTo("Overview"));
            Assert.That(data.GetProperty("leagueTablePositions").GetArrayLength(), Is.EqualTo(1));
            Assert.That(data.GetProperty("matches").GetArrayLength(), Is.EqualTo(0));
        }
    }
}
