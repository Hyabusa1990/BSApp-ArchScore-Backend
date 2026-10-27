using Fawkes.Core.Exceptions;
using NSubstitute;

namespace Fawkes.Core.Abstractions
{
    [TestFixture]
    public class FixtureAccessExtensionsTests
    {
        private const int FixtureId = 5;
        private const string User = "user@example.org";

        private IFawkesDataStore dataStore;

        [SetUp]
        public void Setup()
        {
            dataStore = Substitute.For<IFawkesDataStore>();
        }

        public enum Level { Read, Write, Owner }

        private Task Ensure(Level level) => level switch
        {
            Level.Read => dataStore.EnsureReadAccessAsync(FixtureId, User),
            Level.Write => dataStore.EnsureWriteAccessAsync(FixtureId, User),
            _ => dataStore.EnsureOwnerAccessAsync(FixtureId, User)
        };

        private void GrantAccess(Level level)
        {
            switch (level)
            {
                case Level.Read: dataStore.CheckReadAccessToFixtureAsync(FixtureId, User).Returns(true); break;
                case Level.Write: dataStore.CheckWriteAccessToFixtureAsync(FixtureId, User).Returns(true); break;
                default: dataStore.CheckOwnerAccessToFixtureAsync(FixtureId, User).Returns(true); break;
            }
        }

        [TestCase(Level.Read)]
        [TestCase(Level.Write)]
        [TestCase(Level.Owner)]
        public async Task PassesWithAccess(Level level)
        {
            GrantAccess(level);

            await Ensure(level);

            await dataStore.DidNotReceive().FixtureExistsAsync(Arg.Any<int>());
        }

        [TestCase(Level.Read)]
        [TestCase(Level.Write)]
        [TestCase(Level.Owner)]
        public void ExistingFixtureWithoutAccessIsDenied(Level level)
        {
            dataStore.FixtureExistsAsync(FixtureId).Returns(true);

            var exception = Assert.ThrowsAsync<AccessDeniedException>(() => Ensure(level));

            Assert.That(exception!.Code, Is.EqualTo("ACCESS_DENIED"));
        }

        [TestCase(Level.Read)]
        [TestCase(Level.Write)]
        [TestCase(Level.Owner)]
        public void MissingFixtureIsNotFound(Level level)
        {
            var exception = Assert.ThrowsAsync<NotFoundException>(() => Ensure(level));

            Assert.That(exception!.Code, Is.EqualTo("FIXTURE_NOT_FOUND"));
        }
    }
}
