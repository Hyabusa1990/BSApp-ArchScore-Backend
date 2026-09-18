using Fawkes.Api.Core.Model;
using Fawkes.Api.Core.Rules.MatchPlay;
using Fawkes.Api.Core.Rules.Scoring;

namespace Fawkes.Api.Core.Rules
{
    [TestFixture]
    public class MatchPlayRuleSetTests
    {
        [SetUp]
        public void Setup()
        {

        }

        [TestCase(1, "++++++", "++++++", 1, 1, -1, -1, 4)]
        [TestCase(1, "+++++9", "++++++", 0, 2, -1, -1, 3)]
        [TestCase(1, "+++++9", "+++++8", 2, 0, -1, -1, 3)]
        [TestCase(1, "++++++999999", "++++++999999", 1, 1, -1, -1, 4)]
        [TestCase(2, "++++++999999", "++++++999999", 2, 2, -1, -1, 4)]
        [TestCase(2, "++++++999998", "++++++999999", 1, 3, -1, -1, 4)]
        [TestCase(3, "++++++999998888888", "++++++999999888888", 2, 4, -1, -1, 4)]
        [TestCase(3, "++++++999999888888", "999999888888777777", 6, 0, 2, 0, 3)]
        [TestCase(3, "++++++999999888888", "++++++999999888888", 3, 3, -1, -1, 5)]
        [TestCase(3, "++++++999999888888", "++++++999999888888", 3, 3, -1, -1, 5)]
        [TestCase(3, "++++++999999888888", "++++++999999888888", 3, 3, -1, -1, 5)]
        [TestCase(4, "++++++999998888888000000", "++++++999999888888------", 2, 6, 0, 2, 4)]
        [TestCase(4, "++++++999998888888000001", "++++++999999888888000000", 4, 4, -1, -1, 5)]
        [TestCase(4, "++++++999998888888000001111111", "++++++999999888888000000111111", 4, 4, -1, -1, 5)]
        [TestCase(5, "++++++999998888888000001111111", "++++++999999888888000000111111", 5, 5, 1, 1, 5)]
        [TestCase(5, "++++++999998888888000001111111", "++++++999999888888000000111112", 4, 6, 0, 2, 5)]
        [TestCase(5, "++++++999998888888000001111112", "++++++999999888888000000111111", 6, 4, 2, 0, 5)]

        public void MatchPlayRulesWorkForBestOfFiveSetsWithoutShootOff(int setNo, string shotsA, string shotsB, int aSetPoints, int bSetPoints, int aMatchPoints, int bMatchPoints, int minSetNo)
        {

            var scoringRules = new ScoringRuleSetBase(5, 6, 0, false);
            var matchPlayRules = new SetSystemMatchPlayRuleSetBase(5, false);


            var scoresheetA = scoringRules.EvaluateScoresheet(new TargetData()
            {
                Shots = shotsA
            });

            var scoresheetB = scoringRules.EvaluateScoresheet(new TargetData()
            {
                Shots = shotsB
            });

            for (var i = 1; i <= setNo; i++)
            {
                scoresheetA[i].IsConfirmed = scoresheetB[i].IsConfirmed = true;
            }


            var match = matchPlayRules.EvaluateMatch(scoresheetA, scoresheetB);


            Assert.That(scoresheetA.SetPoints, Is.EqualTo(aSetPoints));
            Assert.That(scoresheetB.SetPoints, Is.EqualTo(bSetPoints));

            if (aMatchPoints >= 0)
                Assert.That(scoresheetA.MatchPoints, Is.EqualTo(aMatchPoints));
            else
                Assert.That(scoresheetA.MatchPoints, Is.Null);

            if (bMatchPoints >= 0)
                Assert.That(scoresheetB.MatchPoints, Is.EqualTo(bMatchPoints));
            else
                Assert.That(scoresheetB.MatchPoints, Is.Null);

            Assert.That(match.MinExpectedNoOfSets, Is.EqualTo(minSetNo));


        }
    }
}
