using Fawkes.Core.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Fawkes.Core.Exceptions;
using Fawkes.Core.Rules;
using Fawkes.Core.Abstractions;
using NSubstitute;

namespace Fawkes.Core.Services
{
    [TestFixture]
    public class DosServiceTests
    {
        private const int FixtureId = 5;
        private const string User = "dos@example.org";

        private IFawkesDataStore dataStore;
        private DosService dosService;
        private FakeLogger<DosService> logger;

        [SetUp]
        public void Setup()
        {
            dataStore = Substitute.For<IFawkesDataStore>();
            dataStore.FixtureExistsAsync(Arg.Any<int>()).Returns(true);
            logger = new FakeLogger<DosService>();
            dosService = new DosService(dataStore, new RuleSetFactory(), logger);
        }

        [Test]
        public void GetRoundMatchesRequiresReadAccess()
        {
            Assert.ThrowsAsync<AccessDeniedException>(() => dosService.GetRoundMatchesAsync(FixtureId, 1, User));
        }

        [Test]
        public void GetRoundMatchesFailsForMissingFixture()
        {
            dataStore.CheckReadAccessToFixtureAsync(FixtureId, User).Returns(true);

            Assert.ThrowsAsync<NotFoundException>(() => dosService.GetRoundMatchesAsync(FixtureId, 1, User));
        }

        [Test]
        public async Task GetRoundMatchesPairsNeighbouringTargets()
        {
            dataStore.CheckReadAccessToFixtureAsync(FixtureId, User).Returns(true);
            dataStore.GetFixtureAsync(FixtureId).Returns(new Fixture() { Id = FixtureId, RuleSetKey = RuleSetKeys.Default });
            dataStore.GetTargetDataAsync(FixtureId, (int?)1, null).Returns(Enumerable.Range(1, 4)
                .Select(targetNo => new TargetData() { RoundNo = 1, TargetNo = targetNo, TeamName = $"Team {targetNo}" })
                .ToArray());

            var matches = (await dosService.GetRoundMatchesAsync(FixtureId, 1, User)).ToList();

            Assert.That(matches, Has.Count.EqualTo(2));
            Assert.That(matches[0].Scoresheets.Select(s => s.TargetNo), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(matches[1].Scoresheets.Select(s => s.TargetNo), Is.EqualTo(new[] { 3, 4 }));
        }

        [Test]
        public async Task SetConfirmedSetScoreRequiresWriteAccess()
        {
            dataStore.CheckReadAccessToFixtureAsync(FixtureId, User).Returns(true);

            Assert.ThrowsAsync<AccessDeniedException>(() => dosService.SetConfirmedSetScore(FixtureId, 1, 2, 3, 54, User));
            await dataStore.DidNotReceiveWithAnyArgs().SaveSetScoreAsync(default, default, default, default, default);
        }

        [Test]
        public async Task SetConfirmedSetScoreSavesScore()
        {
            dataStore.CheckWriteAccessToFixtureAsync(FixtureId, User).Returns(true);

            await dosService.SetConfirmedSetScore(FixtureId, 1, 2, 3, 54, User);

            await dataStore.Received(1).SaveSetScoreAsync(FixtureId, 1, 2, 3, 54);

            Assert.That(logger.LatestRecord.Level, Is.EqualTo(LogLevel.Information));
            Assert.That(logger.LatestRecord.Message, Does.Contain($"set {3} on target {2}, round {1} of fixture {FixtureId} to 54"));
        }
    }
}
