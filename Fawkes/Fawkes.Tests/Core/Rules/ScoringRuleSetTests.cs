using Fawkes.Core.Model;
using Fawkes.Core.Rules.MatchPlay;
using Fawkes.Core.Rules.Scoring;

namespace Fawkes.Core.Rules
{
    [TestFixture]
    public class ScoringRuleSetTests
    {
        private ScoringRuleSetBase scoringRules;

        [SetUp]
        public void Setup()
        {
            scoringRules = new ScoringRuleSetBase(5, 6, 0, false);
        }

        [TestCase("++++++", 60)]
        [TestCase("------", 60)]
        [TestCase("987654", 39)]
        [TestCase("3210Mm", 6)]
        [TestCase("9", 9)]
        [TestCase("9 x?8", 17)]
        public void EvaluateScoreSumsValidShots(string shots, int expectedScore)
        {
            Assert.That(scoringRules.EvaluateScore(shots), Is.EqualTo(expectedScore));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("abc")]
        public void EvaluateScoreReturnsNullWithoutValidShots(string shots)
        {
            Assert.That(scoringRules.EvaluateScore(shots), Is.Null);
        }

        [Test]
        public void EvaluateScoreCountsXAsElevenWhenConfigured()
        {
            var xIsElevenRules = new ScoringRuleSetBase(5, 6, 0, true);

            Assert.That(xIsElevenRules.EvaluateScore("--++99"), Is.EqualTo(60));
        }

        [Test]
        public void EvaluateScoresheetSplitsShotsIntoSets()
        {
            var scoresheet = scoringRules.EvaluateScoresheet(new TargetData()
            {
                RoundNo = 2,
                TargetNo = 3,
                TeamId = 4,
                TeamName = "Team A",
                Shots = "999999888888777"
            });

            Assert.That(scoresheet.RoundNo, Is.EqualTo(2));
            Assert.That(scoresheet.TargetNo, Is.EqualTo(3));
            Assert.That(scoresheet.TeamId, Is.EqualTo(4));
            Assert.That(scoresheet.TeamName, Is.EqualTo("Team A"));

            Assert.That(scoresheet[1].Shots, Is.EqualTo("999999"));
            Assert.That(scoresheet[1].Score, Is.EqualTo(54));
            Assert.That(scoresheet[1].IsComplete, Is.True);

            Assert.That(scoresheet[2].Shots, Is.EqualTo("888888"));
            Assert.That(scoresheet[2].Score, Is.EqualTo(48));

            Assert.That(scoresheet[3].Shots, Is.EqualTo("777"));
            Assert.That(scoresheet[3].Score, Is.EqualTo(21));
            Assert.That(scoresheet[3].NoOfShots, Is.EqualTo(3));
            Assert.That(scoresheet[3].IsComplete, Is.False);

            Assert.That(scoresheet[4].Shots, Is.Empty);
            Assert.That(scoresheet[4].Score, Is.Null);

            Assert.That(scoresheet.TotalScore, Is.EqualTo(123));
        }

        [Test]
        public void EvaluateScoresheetPrefersConfirmedSetScores()
        {
            var scoresheet = scoringRules.EvaluateScoresheet(new TargetData()
            {
                Shots = "999999888888",
                ConfirmedSet01Score = 50
            });

            Assert.That(scoresheet[1].Score, Is.EqualTo(50));
            Assert.That(scoresheet[1].IsConfirmed, Is.True);
            Assert.That(scoresheet[2].Score, Is.EqualTo(48));
            Assert.That(scoresheet[2].IsConfirmed, Is.False);
            Assert.That(scoresheet.TotalScore, Is.EqualTo(98));
        }

        [Test]
        public void EvaluateScoresheetHandlesMissingShots()
        {
            var scoresheet = scoringRules.EvaluateScoresheet(new TargetData()
            {
                Shots = null!
            });

            Assert.That(scoresheet.Sets.All(s => s.Score == null), Is.True);
            Assert.That(scoresheet.TotalScore, Is.EqualTo(0));
        }

        [Test]
        public void EvaluateScoresheetUsesConfirmedScoresWithoutShots()
        {
            var scoresheet = scoringRules.EvaluateScoresheet(new TargetData()
            {
                Shots = string.Empty,
                ConfirmedSet01Score = 55,
                ConfirmedSet02Score = 52
            });

            Assert.That(scoresheet[1].Score, Is.EqualTo(55));
            Assert.That(scoresheet[1].IsConfirmed, Is.True);
            Assert.That(scoresheet[2].Score, Is.EqualTo(52));
            Assert.That(scoresheet[2].IsConfirmed, Is.True);
            Assert.That(scoresheet[3].Score, Is.Null);
            Assert.That(scoresheet[3].IsConfirmed, Is.False);
            Assert.That(scoresheet.TotalScore, Is.EqualTo(107));
        }

        [Test]
        public void EvaluateScoresheetUsesConfirmedScoreForSetBeyondRecordedShots()
        {
            // Shots only cover set 1; the DOS entered set 2 by hand.
            var scoresheet = scoringRules.EvaluateScoresheet(new TargetData()
            {
                Shots = "999999",
                ConfirmedSet01Score = 54,
                ConfirmedSet02Score = 48
            });

            Assert.That(scoresheet[2].Score, Is.EqualTo(48));
            Assert.That(scoresheet[2].IsConfirmed, Is.True);
            Assert.That(scoresheet[2].Shots, Is.Empty);
            Assert.That(scoresheet.TotalScore, Is.EqualTo(102));
        }

        [Test]
        public void MatchIsDecidedByConfirmedScoresWithoutShots()
        {
            var matchRules = new SetSystemMatchPlayRuleSetBase(5, false);
            var winner = new TargetData() { TargetNo = 1, ConfirmedSet01Score = 55, ConfirmedSet02Score = 55, ConfirmedSet03Score = 55 };
            var loser = new TargetData() { TargetNo = 2, ConfirmedSet01Score = 50, ConfirmedSet02Score = 50, ConfirmedSet03Score = 50 };

            var match = matchRules.EvaluateMatch(scoringRules.EvaluateScoresheet(winner), scoringRules.EvaluateScoresheet(loser));

            Assert.That(match.IsComplete, Is.True);
            Assert.That(match.Scoresheets.Select(s => s.MatchPoints), Is.EqualTo(new int?[] { 2, 0 }));
        }
    }
}
