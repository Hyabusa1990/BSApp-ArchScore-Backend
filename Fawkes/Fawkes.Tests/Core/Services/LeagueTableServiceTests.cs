using Fawkes.Core.Abstractions;
using Fawkes.Core.Model;
using Fawkes.Core.Rules;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;

namespace Fawkes.Core.Services
{
    [TestFixture]
    public class LeagueTableServiceTests
    {
        private const int FixtureId = 4;

        private IFawkesDataStore dataStore;
        private LeagueTableService leagueTableService;
        private LeagueTable? storedTable;

        [SetUp]
        public void Setup()
        {
            dataStore = Substitute.For<IFawkesDataStore>();
            dataStore.GetTargetDataAsync(FixtureId, null, null).Returns(Array.Empty<TargetData>());
            dataStore.When(d => d.UpdateLeagueTableAsync(FixtureId, Arg.Any<LeagueTable>()))
                .Do(ci => storedTable = ci.Arg<LeagueTable>());
            leagueTableService = new LeagueTableService(dataStore, new RuleSetFactory(), new FakeLogger<LeagueTableService>());
        }

        private static LeagueTablePosition CreatePosition(int teamId, string teamName, int matchPointsWon, int setPointsWon, int setPointsLost) => new LeagueTablePosition()
        {
            TeamId = teamId,
            TeamName = teamName,
            MatchPointsWon = matchPointsWon,
            SetPointsWon = setPointsWon,
            SetPointsLost = setPointsLost
        };

        [Test]
        public async Task WithoutShotsTheTableIsRankedByTheInitialStandings()
        {
            // Standings from earlier fixtures; the initial ranks are not given (0).
            dataStore.GetInitialLeagueTableAsync(FixtureId).Returns(new LeagueTable()
            {
                Positions = [CreatePosition(1, "A", 4, 20, 22), CreatePosition(2, "B", 10, 30, 12), CreatePosition(3, "C", 4, 22, 20)]
            });

            await leagueTableService.RecalculateAsync(new Fixture() { Id = FixtureId, RuleSetKey = RuleSetKeys.Default });

            Assert.That(storedTable!.Positions.Select(p => p.TeamName), Is.EqualTo(new[] { "B", "C", "A" }));
            Assert.That(storedTable.Positions.Select(p => p.Rank), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public async Task AtSeasonStartAllTeamsShareRankOneInsteadOfZero()
        {
            dataStore.GetInitialLeagueTableAsync(FixtureId).Returns(new LeagueTable()
            {
                Positions = [CreatePosition(1, "A", 0, 0, 0), CreatePosition(2, "B", 0, 0, 0)]
            });

            await leagueTableService.RecalculateAsync(new Fixture() { Id = FixtureId, RuleSetKey = RuleSetKeys.Default });

            Assert.That(storedTable!.Positions.Select(p => p.Rank), Is.All.EqualTo(1));
        }
    }
}
