using Fawkes.Core.Model;
using Fawkes.Core.Exceptions;
using Fawkes.Core.Rules;
using Fawkes.Core.Abstractions;
using NSubstitute;

namespace Fawkes.Core.Services
{
    [TestFixture]
    public class FixtureServiceTests
    {
        private const int FixtureId = 7;
        private const string Owner = "owner@example.org";
        private const string Other = "other@example.org";

        private IFawkesDataStore dataStore;
        private FixtureService fixtureService;

        [SetUp]
        public void Setup()
        {
            dataStore = Substitute.For<IFawkesDataStore>();
            dataStore.FixtureExistsAsync(Arg.Any<int>()).Returns(true);
            fixtureService = new FixtureService(dataStore, new RuleSetFactory());
        }

        [Test]
        public async Task CreateFixtureStoresUtcDateAndGrantsOwnership()
        {
            dataStore.CreateFixtureAsync(Arg.Any<DateTime>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(new Fixture() { Id = FixtureId });

            var fixture = await fixtureService.CreateFixtureAsync(new DateTime(2026, 10, 10), "Kassel", "Hessenliga", "1. Wettkampftag", Owner);

            Assert.That(fixture.Id, Is.EqualTo(FixtureId));
            await dataStore.Received(1).CreateFixtureAsync(
                Arg.Is<DateTime>(d => d.Kind == DateTimeKind.Utc && d == new DateTime(2026, 10, 10)),
                "Kassel", "Hessenliga", "1. Wettkampftag");
            await dataStore.Received(1).GrantFixtureAccessAsync(FixtureId, Owner, AccessLevel.Owner);
        }

        [Test]
        public void GetFixtureRequiresReadAccess()
        {
            Assert.ThrowsAsync<AccessDeniedException>(() => fixtureService.GetFixtureAsync(FixtureId, Other));
        }

        [Test]
        public async Task GetFixtureReturnsFixtureWithReadAccess()
        {
            var fixture = new Fixture() { Id = FixtureId };
            dataStore.CheckReadAccessToFixtureAsync(FixtureId, Owner).Returns(true);
            dataStore.GetFixtureAsync(FixtureId).Returns(fixture);

            Assert.That(await fixtureService.GetFixtureAsync(FixtureId, Owner), Is.SameAs(fixture));
        }

        [Test]
        public void GetFixtureFailsWhenFixtureDisappeared()
        {
            dataStore.CheckReadAccessToFixtureAsync(FixtureId, Owner).Returns(true);

            Assert.ThrowsAsync<NotFoundException>(() => fixtureService.GetFixtureAsync(FixtureId, Owner));
        }

        [Test]
        public async Task SetPhaseRejectsNegativeRoundNo()
        {
            dataStore.CheckWriteAccessToFixtureAsync(FixtureId, Owner).Returns(true);

            Assert.ThrowsAsync<InvalidRequestException>(() => fixtureService.SetPhaseAsync(FixtureId, -1, Owner));
            await dataStore.DidNotReceiveWithAnyArgs().UpdateFixtureAsync(default);
        }

        [Test]
        public void UpdateFixtureRequiresWriteAccess()
        {
            Assert.ThrowsAsync<AccessDeniedException>(() => fixtureService.UpdateFixtureAsync(FixtureId, DateTime.UtcNow, "Kassel", "Hessenliga", "1. Wettkampftag", Other));
        }

        [Test]
        public async Task UpdateFixtureConvertsDateToUtc()
        {
            dataStore.CheckWriteAccessToFixtureAsync(FixtureId, Owner).Returns(true);

            await fixtureService.UpdateFixtureAsync(FixtureId, new DateTime(2026, 10, 10), "Kassel", "Hessenliga", "1. Wettkampftag", Owner);

            await dataStore.Received(1).UpdateFixtureAsync(FixtureId,
                Arg.Is<DateTime?>(d => d!.Value.Kind == DateTimeKind.Utc),
                "Kassel", "Hessenliga", "1. Wettkampftag", null);
        }

        [Test]
        public void DeleteFixtureRequiresOwnerAccess()
        {
            dataStore.CheckWriteAccessToFixtureAsync(FixtureId, Other).Returns(true);

            Assert.ThrowsAsync<AccessDeniedException>(() => fixtureService.DeleteFixtureAsync(FixtureId, Other));
        }

        [Test]
        public async Task DeleteFixtureDeletesAsOwner()
        {
            dataStore.CheckOwnerAccessToFixtureAsync(FixtureId, Owner).Returns(true);

            await fixtureService.DeleteFixtureAsync(FixtureId, Owner);

            await dataStore.Received(1).DeleteFixtureAsync(FixtureId);
        }

        [Test]
        public void GetUsersRequiresReadAccess()
        {
            Assert.ThrowsAsync<AccessDeniedException>(() => fixtureService.GetUsersForFixtureAsync(FixtureId, Other));
        }

        [Test]
        public void AddUserRequiresOwnerAccess()
        {
            Assert.ThrowsAsync<AccessDeniedException>(() => fixtureService.AddUserToFixtureAsync(FixtureId, Other, Other));
        }

        [Test]
        public async Task AddUserGrantsWriteAccess()
        {
            dataStore.CheckOwnerAccessToFixtureAsync(FixtureId, Owner).Returns(true);

            await fixtureService.AddUserToFixtureAsync(FixtureId, Other, Owner);

            await dataStore.Received(1).GrantFixtureAccessAsync(FixtureId, Other, AccessLevel.Write);
        }

        [Test]
        public async Task AddUserDoesNotDowngradeOwner()
        {
            dataStore.CheckOwnerAccessToFixtureAsync(FixtureId, Owner).Returns(true);

            await fixtureService.AddUserToFixtureAsync(FixtureId, Owner, Owner);

            await dataStore.DidNotReceiveWithAnyArgs().GrantFixtureAccessAsync(default, default!, default);
        }

        [Test]
        public void RemoveUserRequiresOwnerAccess()
        {
            Assert.ThrowsAsync<AccessDeniedException>(() => fixtureService.RemoveUserFromFixtureAsync(FixtureId, Owner, Other));
        }

        [Test]
        public void OwnerCannotRemoveThemselves()
        {
            dataStore.CheckOwnerAccessToFixtureAsync(FixtureId, Owner).Returns(true);

            Assert.ThrowsAsync<ConflictException>(() => fixtureService.RemoveUserFromFixtureAsync(FixtureId, Owner, Owner));
        }

        [Test]
        public async Task RemoveUserRevokesAccess()
        {
            dataStore.CheckOwnerAccessToFixtureAsync(FixtureId, Owner).Returns(true);

            await fixtureService.RemoveUserFromFixtureAsync(FixtureId, Other, Owner);

            await dataStore.Received(1).GrantFixtureAccessAsync(FixtureId, Other, AccessLevel.None);
        }

        [Test]
        public void SetPhaseRequiresWriteAccess()
        {
            Assert.ThrowsAsync<AccessDeniedException>(() => fixtureService.SetPhaseAsync(FixtureId, 2, Other));
        }

        [Test]
        public async Task SetPhaseUpdatesRoundAndLeagueTable()
        {
            dataStore.CheckWriteAccessToFixtureAsync(FixtureId, Owner).Returns(true);
            dataStore.GetFixtureAsync(FixtureId).Returns(new Fixture() { Id = FixtureId, RuleSetKey = RuleSetKeys.Default });
            dataStore.GetInitialLeagueTableAsync(FixtureId).Returns(new LeagueTable()
            {
                Positions =
                [
                    new LeagueTablePosition() { TeamId = 1, TeamName = "A", Rank = 1 },
                    new LeagueTablePosition() { TeamId = 2, TeamName = "B", Rank = 2 }
                ]
            });
            dataStore.GetTargetDataAsync(FixtureId, null, null).Returns(new[]
            {
                new TargetData() { RoundNo = 1, TargetNo = 1, TeamId = 2, TeamName = "B", Shots = "999999999999999999", ConfirmedSet01Score = 54, ConfirmedSet02Score = 54, ConfirmedSet03Score = 54 },
                new TargetData() { RoundNo = 1, TargetNo = 2, TeamId = 1, TeamName = "A", Shots = "++++++++++++++++++", ConfirmedSet01Score = 60, ConfirmedSet02Score = 60, ConfirmedSet03Score = 60 }
            });

            await fixtureService.SetPhaseAsync(FixtureId, 2, Owner);

            await dataStore.Received(1).UpdateFixtureAsync(FixtureId, null, null, null, null, 2);
            await dataStore.Received(1).UpdateLeagueTableAsync(FixtureId, Arg.Is<LeagueTable>(t =>
                t.Positions.First().TeamName == "A" &&
                t.Positions.First().MatchPointsWon == 2 &&
                t.Positions.First().SetPointsWon == 6 &&
                t.Positions.Last().MatchPointsLost == 2));
        }

        [Test]
        public void SetPhaseFailsForMissingFixture()
        {
            dataStore.CheckWriteAccessToFixtureAsync(FixtureId, Owner).Returns(true);

            Assert.ThrowsAsync<NotFoundException>(() => fixtureService.SetPhaseAsync(FixtureId, 2, Owner));
        }
    }
}
