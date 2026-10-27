using Fawkes.Core.Model;
using Fawkes.Core.Exceptions;
using Fawkes.Core.Rules;
using Fawkes.Core.Abstractions;
using NSubstitute;

namespace Fawkes.Core.Services
{
    [TestFixture]
    public class DisplayServiceTests
    {
        private const string DeviceCode = "123456";

        private IFawkesDataStore dataStore;
        private DisplayService displayService;

        [SetUp]
        public void Setup()
        {
            dataStore = Substitute.For<IFawkesDataStore>();
            displayService = new DisplayService(dataStore, new RuleSetFactory());
        }

        private void SetupDevice(DisplayType displayType, int? fixtureId = 3)
        {
            dataStore.GetDeviceByCodeAsync(DeviceCode).Returns(new Device()
            {
                Code = DeviceCode,
                FixtureId = fixtureId,
                DisplayType = displayType,
                DisplayTheme = DisplayTheme.Light
            });
        }

        private void SetupFixture()
        {
            dataStore.GetFixtureAsync(3).Returns(new Fixture() { Id = 3, CurrentRoundNo = 1, RuleSetKey = RuleSetKeys.Default });
        }

        [Test]
        public async Task UnknownDeviceReturnsNull()
        {
            Assert.That(await displayService.GetDisplayDataAsync(DeviceCode), Is.Null);
        }

        [Test]
        public async Task UnassignedDeviceShowsItsCode()
        {
            SetupDevice(DisplayType.Match, fixtureId: null);

            var data = await displayService.GetDisplayDataAsync(DeviceCode);

            Assert.That(data, Is.TypeOf<UnassignedDisplayData>());
            Assert.That(((UnassignedDisplayData)data!).DeviceCode, Is.EqualTo(DeviceCode));
            Assert.That(data.Theme, Is.EqualTo(DisplayTheme.Light));
        }

        [Test]
        public async Task DeviceOfDeletedFixtureShowsItsCodeAgain()
        {
            SetupDevice(DisplayType.Match);

            var data = await displayService.GetDisplayDataAsync(DeviceCode);

            Assert.That(data, Is.TypeOf<UnassignedDisplayData>());
            Assert.That(((UnassignedDisplayData)data!).DeviceCode, Is.EqualTo(DeviceCode));
        }

        [Test]
        public async Task MatchDisplayWithoutTargetsShowsNothing()
        {
            SetupDevice(DisplayType.Match);
            SetupFixture();
            dataStore.GetTargetDataAsync(3, (int?)1, (int?)1).Returns(Array.Empty<TargetData>());

            var data = await displayService.GetDisplayDataAsync(DeviceCode);

            Assert.That(data, Is.TypeOf<NoDisplayData>());
            Assert.That(data!.Theme, Is.EqualTo(DisplayTheme.Light));
        }

        [Test]
        public async Task DisplayTypeNoneReturnsEmptyData()
        {
            SetupDevice(DisplayType.None);
            SetupFixture();

            Assert.That(await displayService.GetDisplayDataAsync(DeviceCode), Is.TypeOf<NoDisplayData>());
        }

        [Test]
        public async Task TableDisplayReturnsLeagueTable()
        {
            var positions = new[] { new LeagueTablePosition() { TeamId = 1, TeamName = "A", Rank = 1 } };
            SetupDevice(DisplayType.Table);
            SetupFixture();
            dataStore.GetLeagueTableAsync(3).Returns(new LeagueTable() { Positions = positions });

            var data = await displayService.GetDisplayDataAsync(DeviceCode);

            Assert.That(data, Is.TypeOf<TableDisplayData>());
            Assert.That(((TableDisplayData)data!).Positions, Is.EqualTo(positions));
            Assert.That(data.Theme, Is.EqualTo(DisplayTheme.Light));
        }

        [Test]
        public async Task MatchDisplayShowsCurrentSetOfBothTargets()
        {
            SetupDevice(DisplayType.Match);
            SetupFixture();
            dataStore.GetTargetDataAsync(3, (int?)1, (int?)1).Returns(new[]
            {
                new TargetData() { TargetNo = 1, TeamName = "A", Shots = "999999888888", ConfirmedSet01Score = 54 },
                new TargetData() { TargetNo = 2, TeamName = "B", Shots = "888888999999", ConfirmedSet01Score = 48 }
            });

            var data = await displayService.GetDisplayDataAsync(DeviceCode);

            Assert.That(data, Is.TypeOf<MatchDisplayData>());
            var matchData = (MatchDisplayData)data!;
            Assert.That(matchData.MinNoOfSets, Is.EqualTo(3));

            var targets = matchData.Targets.ToList();
            Assert.That(targets.Select(t => t.TeamName), Is.EqualTo(new[] { "A", "B" }));
            Assert.That(targets.Select(t => t.CurrentSetNo), Is.All.EqualTo(2));
            Assert.That(targets.Select(t => t.IsConfirmed), Is.All.False);

            Assert.That(targets[0].Shots, Is.EqualTo("888888"));
            Assert.That(targets[0].CurrentSetScore, Is.EqualTo(48));
            Assert.That(targets[0].SetScores, Is.EqualTo(new int?[] { 54, 48 }));
            Assert.That(targets[0].SetPoints, Is.EqualTo(2));

            Assert.That(targets[1].Shots, Is.EqualTo("999999"));
            Assert.That(targets[1].CurrentSetScore, Is.EqualTo(54));
            Assert.That(targets[1].SetPoints, Is.EqualTo(0));
        }
    }
}
