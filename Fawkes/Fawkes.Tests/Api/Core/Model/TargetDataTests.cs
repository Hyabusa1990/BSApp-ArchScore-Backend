namespace Fawkes.Api.Core.Model
{
    [TestFixture]
    public class TargetDataTests
    {
        [Test]
        public void CurrentSetNoIsOneWithoutConfirmedSets()
        {
            Assert.That(new TargetData().CurrentSetNo, Is.EqualTo(1));
        }

        [Test]
        public void CurrentSetNoFollowsLastConfirmedSet()
        {
            var target = new TargetData() { ConfirmedSet01Score = 54, ConfirmedSet02Score = 48 };

            Assert.That(target.CurrentSetNo, Is.EqualTo(3));
        }

        [Test]
        public void CurrentSetNoStopsAtFirstGap()
        {
            var target = new TargetData() { ConfirmedSet02Score = 48 };

            Assert.That(target.CurrentSetNo, Is.EqualTo(1));
        }

        [Test]
        public void CurrentSetNoIsSixWhenAllSetsAreConfirmed()
        {
            var target = new TargetData()
            {
                ConfirmedSet01Score = 50,
                ConfirmedSet02Score = 50,
                ConfirmedSet03Score = 50,
                ConfirmedSet04Score = 50,
                ConfirmedSet05Score = 50
            };

            Assert.That(target.CurrentSetNo, Is.EqualTo(6));
        }
    }
}
