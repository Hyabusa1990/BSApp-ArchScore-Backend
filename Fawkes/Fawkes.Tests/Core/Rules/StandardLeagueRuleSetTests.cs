using Fawkes.Core.Model;
using Fawkes.Core.Rules.League;

namespace Fawkes.Core.Rules
{
    [TestFixture]
    public class StandardLeagueRuleSetTests
    {
        private StandardLeagueRuleSet leagueRules;

        [SetUp]
        public void Setup()
        {
            leagueRules = new StandardLeagueRuleSet();
        }

        private static Scoresheet CreateScoresheet(int teamId, string teamName, int setPoints, int? matchPoints, int totalScore)
        {
            var scoresheet = new Scoresheet(5, 6, 0)
            {
                TeamId = teamId,
                TeamName = teamName,
                SetPoints = setPoints,
                MatchPoints = matchPoints
            };
            scoresheet[1].Score = totalScore;
            return scoresheet;
        }

        private static Match CreateMatch(params Scoresheet[] scoresheets) => new Match() { Scoresheets = scoresheets };

        private static LeagueTablePosition CreatePosition(int teamId, string teamName, int rank) => new LeagueTablePosition()
        {
            TeamId = teamId,
            TeamName = teamName,
            Rank = rank
        };

        [Test]
        public void CalculateLeagueTableSumsPointsAndRanksTeams()
        {
            var initialTable = new LeagueTable()
            {
                Positions = [CreatePosition(1, "A", 1), CreatePosition(2, "B", 2), CreatePosition(3, "C", 3), CreatePosition(4, "D", 4)]
            };

            var table = leagueRules.CalculateLeagueTable(initialTable,
                CreateMatch(CreateScoresheet(1, "A", 6, 2, 280), CreateScoresheet(2, "B", 2, 0, 270)),
                CreateMatch(CreateScoresheet(3, "C", 5, 1, 275), CreateScoresheet(4, "D", 5, 1, 260)));

            var positions = table.Positions.ToList();

            Assert.That(positions.Select(p => p.TeamName), Is.EqualTo(new[] { "A", "C", "D", "B" }));
            Assert.That(positions.Select(p => p.Rank), Is.EqualTo(new[] { 1, 2, 2, 4 }));
            Assert.That(positions.Select(p => p.RankDifference), Is.All.EqualTo(0));

            var a = positions[0];
            Assert.That(a.MatchPointsWon, Is.EqualTo(2));
            Assert.That(a.MatchPointsLost, Is.EqualTo(0));
            Assert.That(a.SetPointsWon, Is.EqualTo(6));
            Assert.That(a.SetPointsLost, Is.EqualTo(2));
            Assert.That(a.TotalScore, Is.EqualTo(280));

            var b = positions[3];
            Assert.That(b.MatchPointsWon, Is.EqualTo(0));
            Assert.That(b.MatchPointsLost, Is.EqualTo(2));
            Assert.That(b.SetPointsDifference, Is.EqualTo(-4));
        }

        [Test]
        public void CalculateLeagueTableAddsToInitialTableAndTracksRankDifference()
        {
            var initialTable = new LeagueTable()
            {
                Positions =
                [
                    new LeagueTablePosition() { TeamId = 1, TeamName = "A", Rank = 1, MatchPointsWon = 2, SetPointsWon = 6, SetPointsLost = 2 },
                    new LeagueTablePosition() { TeamId = 2, TeamName = "B", Rank = 2, MatchPointsLost = 2, SetPointsWon = 2, SetPointsLost = 6 }
                ]
            };

            var table = leagueRules.CalculateLeagueTable(initialTable,
                CreateMatch(CreateScoresheet(1, "A", 0, 0, 250), CreateScoresheet(2, "B", 6, 2, 280)));

            var positions = table.Positions.ToList();

            Assert.That(positions.Select(p => p.TeamName), Is.EqualTo(new[] { "B", "A" }));
            Assert.That(positions[0].Rank, Is.EqualTo(1));
            Assert.That(positions[0].RankDifference, Is.EqualTo(-1));
            Assert.That(positions[0].MatchPointsWon, Is.EqualTo(2));
            Assert.That(positions[0].SetPointsDifference, Is.EqualTo(2));
            Assert.That(positions[1].Rank, Is.EqualTo(2));
            Assert.That(positions[1].RankDifference, Is.EqualTo(1));
            Assert.That(positions[1].SetPointsDifference, Is.EqualTo(-2));
        }

        [Test]
        public void CalculateLeagueTableOrdersTiedTeamsByName()
        {
            var initialTable = new LeagueTable()
            {
                Positions = [CreatePosition(1, "Zeta", 1), CreatePosition(2, "Alpha", 2)]
            };

            var table = leagueRules.CalculateLeagueTable(initialTable);

            Assert.That(table.Positions.Select(p => p.TeamName), Is.EqualTo(new[] { "Alpha", "Zeta" }));
            Assert.That(table.Positions.Select(p => p.Rank), Is.All.EqualTo(1));
        }
    }
}
