namespace Fawkes.Api.Core.Model
{
    [TestFixture]
    public class ScoresheetTests
    {
        [TestCase(0)]
        [TestCase(6)]
        public void IndexerRejectsSetNumbersOutOfRange(int setNo)
        {
            var scoresheet = new Scoresheet(5, 6, 0);

            Assert.Throws<ArgumentOutOfRangeException>(() => _ = scoresheet[setNo]);
        }

        [Test]
        public void AllShotsPadsEverySetToFullLength()
        {
            var scoresheet = new Scoresheet(5, 6, 0);
            scoresheet[1].Shots = "999999";
            scoresheet[2].Shots = "+++";

            Assert.That(scoresheet.AllShots, Is.EqualTo("999999+++".PadRight(30)));
        }

        [Test]
        public void TotalScoreIgnoresSetsWithoutScore()
        {
            var scoresheet = new Scoresheet(5, 6, 0);
            scoresheet[1].Score = 54;
            scoresheet[3].Score = 48;

            Assert.That(scoresheet.TotalScore, Is.EqualTo(102));
        }

        [Test]
        public void SetIsCompleteOnceAllShotsAreIn()
        {
            var scoresheet = new Scoresheet(5, 6, 0);
            scoresheet[1].NoOfShots = 5;
            scoresheet[2].NoOfShots = 6;

            Assert.That(scoresheet[1].IsComplete, Is.False);
            Assert.That(scoresheet[2].IsComplete, Is.True);
        }
    }
}
