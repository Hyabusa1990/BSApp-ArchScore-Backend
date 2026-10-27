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
    public class SpotterServiceTests
    {
        private const int FixtureId = 4;
        private const int RoundNo = 2;
        private const int TargetNo = 3;
        private static readonly Guid FixtureUniqueId = Guid.Parse("6f1c2b7e-3d4a-4c5b-9e8f-0a1b2c3d4e5f");

        private IFawkesDataStore dataStore;
        private SpotterService spotterService;
        private FakeLogger<SpotterService> logger;

        [SetUp]
        public void Setup()
        {
            dataStore = Substitute.For<IFawkesDataStore>();
            logger = new FakeLogger<SpotterService>();
            spotterService = new SpotterService(dataStore, new RuleSetFactory(), logger);
        }

        private void SetupFixture(int currentRoundNo = RoundNo)
        {
            dataStore.GetFixtureByUniqueIdAsync(FixtureUniqueId).Returns(new Fixture()
            {
                Id = FixtureId,
                UniqueId = FixtureUniqueId,
                CurrentRoundNo = currentRoundNo,
                RuleSetKey = RuleSetKeys.Default
            });
        }

        private void SetupTarget(string shots, int? confirmedSet01Score = null)
        {
            dataStore.GetTargetDataAsync(FixtureId, RoundNo, TargetNo).Returns(new TargetData()
            {
                RoundNo = RoundNo,
                TargetNo = TargetNo,
                TeamName = "Team A",
                Shots = shots,
                ConfirmedSet01Score = confirmedSet01Score
            });
        }

        private static TargetData CreateTarget(int targetNo, string shots, params int[] confirmedScores) => new TargetData()
        {
            RoundNo = RoundNo,
            TargetNo = targetNo,
            TeamName = $"Team {targetNo}",
            Shots = shots,
            ConfirmedSet01Score = confirmedScores.Length > 0 ? confirmedScores[0] : null,
            ConfirmedSet02Score = confirmedScores.Length > 1 ? confirmedScores[1] : null,
            ConfirmedSet03Score = confirmedScores.Length > 2 ? confirmedScores[2] : null,
            ConfirmedSet04Score = confirmedScores.Length > 3 ? confirmedScores[3] : null,
            ConfirmedSet05Score = confirmedScores.Length > 4 ? confirmedScores[4] : null
        };

        // Target 3 plays against target 4 (match 2); the spotter's target is also returned as single target.
        private void SetupMatch(TargetData spotterTarget, TargetData opponentTarget)
        {
            dataStore.GetTargetDataAsync(FixtureId, RoundNo, TargetNo).Returns(spotterTarget);
            dataStore.GetTargetDataAsync(FixtureId, (int?)RoundNo, (int?)2).Returns(new[] { spotterTarget, opponentTarget });
        }

        private void SetupFinishedTarget()
        {
            dataStore.GetTargetDataAsync(FixtureId, RoundNo, TargetNo).Returns(new TargetData()
            {
                RoundNo = RoundNo,
                TargetNo = TargetNo,
                TeamName = "Team A",
                Shots = "999999888888777777999999888888",
                ConfirmedSet01Score = 54,
                ConfirmedSet02Score = 48,
                ConfirmedSet03Score = 42,
                ConfirmedSet04Score = 54,
                ConfirmedSet05Score = 48
            });
        }

        [Test]
        public void UnknownFixtureThrows()
        {
            Assert.ThrowsAsync<NotFoundException>(() => spotterService.GetTargetDataAsync(FixtureUniqueId, TargetNo));
            Assert.ThrowsAsync<NotFoundException>(() => spotterService.ProcessShotsAsync(FixtureUniqueId, TargetNo, "9"));
            Assert.ThrowsAsync<NotFoundException>(() => spotterService.ConfirmEndScoreAsync(FixtureUniqueId, TargetNo));
        }

        [Test]
        public void FixtureWithoutStartedRoundThrows()
        {
            SetupFixture(currentRoundNo: 0);

            Assert.ThrowsAsync<ConflictException>(() => spotterService.GetTargetDataAsync(FixtureUniqueId, TargetNo));
            Assert.ThrowsAsync<ConflictException>(() => spotterService.ProcessShotsAsync(FixtureUniqueId, TargetNo, "9"));
            Assert.ThrowsAsync<ConflictException>(() => spotterService.ConfirmEndScoreAsync(FixtureUniqueId, TargetNo));
        }

        [Test]
        public void UnassignedTargetThrows()
        {
            SetupFixture();

            Assert.ThrowsAsync<NotFoundException>(() => spotterService.GetTargetDataAsync(FixtureUniqueId, TargetNo));
        }

        [Test]
        public async Task GetTargetDataReturnsCurrentSet()
        {
            SetupFixture();
            SetupTarget("999999888", confirmedSet01Score: 54);

            var data = await spotterService.GetTargetDataAsync(FixtureUniqueId, TargetNo);

            Assert.That(data.TargetNo, Is.EqualTo(TargetNo));
            Assert.That(data.TeamName, Is.EqualTo("Team A"));
            Assert.That(data.CurrentSetNo, Is.EqualTo(2));
            Assert.That(data.Shots, Is.EqualTo("888"));
            Assert.That(data.CurrentSetScore, Is.EqualTo(24));
            Assert.That(data.IsConfirmed, Is.False);
        }

        [Test]
        public async Task ProcessShotsReplacesShotsOfCurrentSet()
        {
            SetupFixture();
            SetupTarget("999999888", confirmedSet01Score: 54);

            var data = await spotterService.ProcessShotsAsync(FixtureUniqueId, TargetNo, "++8");

            Assert.That(data.CurrentSetNo, Is.EqualTo(2));
            Assert.That(data.Shots, Is.EqualTo("++8"));
            Assert.That(data.CurrentSetScore, Is.EqualTo(28));
            await dataStore.Received(1).SaveShotsAsync(FixtureId, RoundNo, TargetNo, "999999++8".PadRight(30));

            // Shot entries happen for every arrow, so they stay at debug level.
            Assert.That(logger.Collector.GetSnapshot().Select(r => r.Level), Is.All.EqualTo(LogLevel.Debug));
            Assert.That(logger.Collector.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetTargetDataStaysOnLastSetWhenAllSetsAreConfirmed()
        {
            SetupFixture();
            SetupFinishedTarget();

            var data = await spotterService.GetTargetDataAsync(FixtureUniqueId, TargetNo);

            Assert.That(data.CurrentSetNo, Is.EqualTo(5));
            Assert.That(data.Shots, Is.EqualTo("888888"));
            Assert.That(data.CurrentSetScore, Is.EqualTo(48));
            Assert.That(data.IsConfirmed, Is.True);
        }

        [Test]
        public async Task ProcessShotsFailsWhenAllSetsAreConfirmed()
        {
            SetupFixture();
            SetupFinishedTarget();

            Assert.ThrowsAsync<ConflictException>(() => spotterService.ProcessShotsAsync(FixtureUniqueId, TargetNo, "++++++"));
            await dataStore.DidNotReceiveWithAnyArgs().SaveShotsAsync(default, default, default, default!);
        }

        [Test]
        public async Task ConfirmFailsWhenAllSetsAreConfirmed()
        {
            SetupFixture();
            SetupFinishedTarget();

            Assert.ThrowsAsync<ConflictException>(() => spotterService.ConfirmEndScoreAsync(FixtureUniqueId, TargetNo));
            await dataStore.DidNotReceiveWithAnyArgs().SaveConfirmedSetScoreAsync(default, default, default, default, default);
        }

        [Test]
        public void ConfirmFailsWithoutShots()
        {
            SetupFixture();
            SetupTarget("999999", confirmedSet01Score: 54);

            Assert.ThrowsAsync<ConflictException>(() => spotterService.ConfirmEndScoreAsync(FixtureUniqueId, TargetNo));
        }

        [Test]
        public async Task ConfirmSavesScoreOfCurrentSet()
        {
            SetupFixture();
            SetupTarget("999999888888", confirmedSet01Score: 54);

            var data = await spotterService.ConfirmEndScoreAsync(FixtureUniqueId, TargetNo);

            Assert.That(data.CurrentSetNo, Is.EqualTo(2));
            Assert.That(data.CurrentSetScore, Is.EqualTo(48));
            Assert.That(data.IsConfirmed, Is.True);
            await dataStore.Received(1).SaveConfirmedSetScoreAsync(FixtureId, RoundNo, TargetNo, 2, 48);
        }

        [Test]
        public async Task MatchIsNotCompleteWhileUndecided()
        {
            SetupFixture();
            SetupMatch(CreateTarget(TargetNo, "++++++999", 60), CreateTarget(4, "999999888", 54));

            Assert.That((await spotterService.GetTargetDataAsync(FixtureUniqueId, TargetNo)).MatchComplete, Is.False);
            Assert.That((await spotterService.ProcessShotsAsync(FixtureUniqueId, TargetNo, "+++")).MatchComplete, Is.False);
        }

        [Test]
        public async Task MatchIsCompleteOnceOneTeamHasMoreThanFiveSetPoints()
        {
            SetupFixture();
            SetupMatch(CreateTarget(TargetNo, "++++++++++++++++++", 60, 60, 60), CreateTarget(4, "999999999999999999", 54, 54, 54));

            var data = await spotterService.GetTargetDataAsync(FixtureUniqueId, TargetNo);

            Assert.That(data.MatchComplete, Is.True);
        }

        [Test]
        public async Task MatchIsCompleteAfterDrawOverFiveSets()
        {
            SetupFixture();
            SetupMatch(CreateTarget(TargetNo, "++++++999999++++++99999988888+", 60, 54, 60, 54, 50),
                       CreateTarget(4, "999999++++++999999++++++8888+8", 54, 60, 54, 60, 50));

            Assert.That((await spotterService.GetTargetDataAsync(FixtureUniqueId, TargetNo)).MatchComplete, Is.True);
        }

        [Test]
        public async Task ConfirmingTheDecidingSetCompletesTheMatch()
        {
            SetupFixture();
            SetupMatch(CreateTarget(TargetNo, "++++++++++++++++++", 60, 60), CreateTarget(4, "999999999999999999", 54, 54, 54));

            var data = await spotterService.ConfirmEndScoreAsync(FixtureUniqueId, TargetNo);

            Assert.That(data.CurrentSetNo, Is.EqualTo(3));
            Assert.That(data.IsConfirmed, Is.True);
            Assert.That(data.MatchComplete, Is.True);
            await dataStore.Received(1).SaveConfirmedSetScoreAsync(FixtureId, RoundNo, TargetNo, 3, 60);

            var messages = logger.Collector.GetSnapshot().Where(r => r.Level == LogLevel.Information).Select(r => r.Message).ToList();
            Assert.That(messages, Has.Count.EqualTo(2));
            Assert.That(messages[0], Does.Contain($"confirmed set 3 of Team {TargetNo} on target {TargetNo}, round {RoundNo} of fixture {FixtureId} with score 60"));
            Assert.That(messages[1], Does.Contain($"Match 2 in round {RoundNo} of fixture {FixtureId} is complete"));
        }

        [Test]
        public async Task ConfirmingWhileOpponentIsBehindLeavesMatchOpen()
        {
            SetupFixture();
            SetupMatch(CreateTarget(TargetNo, "++++++++++++++++++", 60, 60), CreateTarget(4, "999999999999", 54, 54));

            var data = await spotterService.ConfirmEndScoreAsync(FixtureUniqueId, TargetNo);

            Assert.That(data.IsConfirmed, Is.True);
            Assert.That(data.MatchComplete, Is.False);
        }
    }
}
