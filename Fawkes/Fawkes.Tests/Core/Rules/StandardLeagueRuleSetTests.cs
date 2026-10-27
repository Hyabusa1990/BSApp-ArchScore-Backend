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

        private static Scoresheet CreateScoresheet(int teamId, string teamName, int setPoints, int? matchPoints, int totalScore, int roundNo = 1)
        {
            var scoresheet = new Scoresheet(5, 6, 0)
            {
                RoundNo = roundNo,
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
        public void RoundWithUnfinishedMatchIsLeftOutCompletely()
        {
            var initialTable = new LeagueTable()
            {
                Positions = [CreatePosition(1, "A", 1), CreatePosition(2, "B", 2), CreatePosition(3, "C", 3), CreatePosition(4, "D", 4)]
            };

            var table = leagueRules.CalculateLeagueTable(initialTable,
                // Round 1: both matches finished
                CreateMatch(CreateScoresheet(1, "A", 6, 2, 280, roundNo: 1), CreateScoresheet(2, "B", 2, 0, 270, roundNo: 1)),
                CreateMatch(CreateScoresheet(3, "C", 6, 2, 275, roundNo: 1), CreateScoresheet(4, "D", 0, 0, 250, roundNo: 1)),
                // Round 2: C beat A, but B against D is still running
                CreateMatch(CreateScoresheet(3, "C", 6, 2, 290, roundNo: 2), CreateScoresheet(1, "A", 0, 0, 260, roundNo: 2)),
                CreateMatch(CreateScoresheet(2, "B", 4, null, 220, roundNo: 2), CreateScoresheet(4, "D", 2, null, 210, roundNo: 2)));

            var positions = table.Positions.ToDictionary(p => p.TeamName);

            // Only round 1 counts, including for the finished match of round 2.
            Assert.That(positions["A"].MatchPointsWon, Is.EqualTo(2));
            Assert.That(positions["A"].MatchPointsLost, Is.EqualTo(0));
            Assert.That(positions["A"].TotalScore, Is.EqualTo(280));
            Assert.That(positions["C"].MatchPointsWon, Is.EqualTo(2));
            Assert.That(positions["C"].SetPointsWon, Is.EqualTo(6));
            Assert.That(positions["B"].SetPointsWon, Is.EqualTo(2));
            Assert.That(positions["D"].SetPointsLost, Is.EqualTo(6));
        }

        [Test]
        public void WithoutFinishedRoundTheInitialTableStays()
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
                CreateMatch(CreateScoresheet(1, "A", 0, null, 100), CreateScoresheet(2, "B", 4, null, 110)));

            var positions = table.Positions.ToList();
            Assert.That(positions.Select(p => p.TeamName), Is.EqualTo(new[] { "A", "B" }));
            Assert.That(positions.Select(p => p.MatchPointsWon), Is.EqualTo(new[] { 2, 0 }));
            Assert.That(positions.Select(p => p.SetPointsWon), Is.EqualTo(new[] { 6, 2 }));
            Assert.That(positions.Select(p => p.RankDifference), Is.All.EqualTo(0));
        }

        [Test]
        public void MatchIsCompleteOnceAllTeamsHaveMatchPoints()
        {
            Assert.That(CreateMatch(CreateScoresheet(1, "A", 6, 2, 0), CreateScoresheet(2, "B", 2, 0, 0)).IsComplete, Is.True);
            Assert.That(CreateMatch(CreateScoresheet(1, "A", 4, null, 0), CreateScoresheet(2, "B", 2, null, 0)).IsComplete, Is.False);
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
