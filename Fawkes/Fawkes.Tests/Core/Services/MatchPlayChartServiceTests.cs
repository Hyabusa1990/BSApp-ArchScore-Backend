using Fawkes.Core.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Fawkes.Core.Exceptions;
using Fawkes.Core.Abstractions;
using NSubstitute;

namespace Fawkes.Core.Services
{
    [TestFixture]
    public class MatchPlayChartServiceTests
    {
        private const int FixtureId = 9;
        private const string User = "owner@example.org";

        private IFawkesDataStore dataStore;
        private MatchPlayChartService chartService;
        private FakeLogger<MatchPlayChartService> logger;

        [SetUp]
        public void Setup()
        {
            dataStore = Substitute.For<IFawkesDataStore>();
            logger = new FakeLogger<MatchPlayChartService>();
            chartService = new MatchPlayChartService(dataStore, logger);

            // Stored team ids are 100 + team number, e.g. "T3" gets id 103.
            dataStore.AddTeamToFixtureAsync(FixtureId, Arg.Any<Team>())
                .Returns(ci => new Team() { Id = 100 + int.Parse(ci.Arg<Team>().Name[1..]) });
        }

        private void SetupWritableFixture()
        {
            dataStore.CheckWriteAccessToFixtureAsync(FixtureId, User).Returns(true);
            dataStore.FixtureExistsAsync(FixtureId).Returns(true);
        }

        private static Team[] CreateTeams(int count) => Enumerable.Range(1, count).Select(i => new Team() { Name = $"T{i}" }).ToArray();

        [Test]
        public void CreateChartRequiresWriteAccess()
        {
            dataStore.FixtureExistsAsync(FixtureId).Returns(true);

            Assert.ThrowsAsync<AccessDeniedException>(() => chartService.CreateMatchPlayChartAsync(FixtureId, CreateTeams(8), null, false, User));
        }

        [Test]
        public void CreateChartFailsForMissingFixture()
        {
            // The data store reports no access for fixtures that do not exist.
            dataStore.FixtureExistsAsync(FixtureId).Returns(false);

            Assert.ThrowsAsync<NotFoundException>(() => chartService.CreateMatchPlayChartAsync(FixtureId, CreateTeams(8), null, false, User));
        }

        [Test]
        public async Task CreateChartRefusesToOverwriteWithoutHardOverride()
        {
            SetupWritableFixture();
            dataStore.MatchPlayChartExistsAsync(FixtureId).Returns(true);

            Assert.ThrowsAsync<ConflictException>(() => chartService.CreateMatchPlayChartAsync(FixtureId, CreateTeams(8), null, false, User));
            await dataStore.DidNotReceive().DeleteMatchPlayChartAsync(FixtureId);
        }

        [Test]
        public async Task CreateChartReplacesExistingChartWithHardOverride()
        {
            SetupWritableFixture();
            dataStore.MatchPlayChartExistsAsync(FixtureId).Returns(true);

            await chartService.CreateMatchPlayChartAsync(FixtureId, CreateTeams(8), null, true, User);

            await dataStore.Received(1).DeleteMatchPlayChartAsync(FixtureId);
        }

        [Test]
        public async Task CreateChartUsesDefaultChartForEightTeams()
        {
            SetupWritableFixture();
            var teams = CreateTeams(8);

            await chartService.CreateMatchPlayChartAsync(FixtureId, teams, null, false, User);

            Assert.That(teams.Select(t => t.Id), Is.EqualTo(Enumerable.Range(101, 8)));
            await dataStore.Received(56).AssignTeamToTargetAsync(FixtureId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>());
            await dataStore.Received(1).AssignTeamToTargetAsync(FixtureId, 1, 1, 105);
            await dataStore.Received(1).AssignTeamToTargetAsync(FixtureId, 7, 8, 108);

            Assert.That(logger.LatestRecord.Message, Does.Contain("8 teams, 7 rounds, default chart"));
        }

        [Test]
        public async Task CreateChartLeavesTargetOfMissingEighthTeamEmpty()
        {
            SetupWritableFixture();

            await chartService.CreateMatchPlayChartAsync(FixtureId, CreateTeams(7), null, false, User);

            await dataStore.Received(49).AssignTeamToTargetAsync(FixtureId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>());
            await dataStore.DidNotReceive().AssignTeamToTargetAsync(FixtureId, Arg.Any<int>(), Arg.Any<int>(), 108);
        }

        [Test]
        public async Task CreateChartFailsWithoutDefaultChartForTeamCount()
        {
            SetupWritableFixture();

            Assert.ThrowsAsync<InvalidRequestException>(() => chartService.CreateMatchPlayChartAsync(FixtureId, CreateTeams(5), null, false, User));
            await dataStore.DidNotReceiveWithAnyArgs().AddTeamToFixtureAsync(default, default!);
        }

        [Test]
        public async Task CreateChartUsesGivenTargetAssignments()
        {
            SetupWritableFixture();

            await chartService.CreateMatchPlayChartAsync(FixtureId, CreateTeams(2), [[2, 1], [1, 2]], false, User);

            await dataStore.Received(4).AssignTeamToTargetAsync(FixtureId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>());
            await dataStore.Received(1).AssignTeamToTargetAsync(FixtureId, 1, 1, 102);
            await dataStore.Received(1).AssignTeamToTargetAsync(FixtureId, 1, 2, 101);
            await dataStore.Received(1).AssignTeamToTargetAsync(FixtureId, 2, 1, 101);
            await dataStore.Received(1).AssignTeamToTargetAsync(FixtureId, 2, 2, 102);
        }
    }
}
