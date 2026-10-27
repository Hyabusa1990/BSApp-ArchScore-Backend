using Fawkes.Core.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Fawkes.Core.Exceptions;
using Fawkes.Core.Abstractions;
using Fawkes.Core.Utils;
using NSubstitute;

namespace Fawkes.Core.Services
{
    [TestFixture]
    public class DeviceServiceTests
    {
        private const int FixtureId = 1;
        private const string User = "user@example.org";

        private IFawkesDataStore dataStore;
        private DeviceService deviceService;
        private FakeLogger<DeviceService> logger;

        [SetUp]
        public void Setup()
        {
            dataStore = Substitute.For<IFawkesDataStore>();
            dataStore.FixtureExistsAsync(Arg.Any<int>()).Returns(true);
            logger = new FakeLogger<DeviceService>();
            deviceService = new DeviceService(dataStore, new RandomService(42), logger);
        }

        [Test]
        public async Task RegisterNewDeviceRetriesUntilCodeIsUnused()
        {
            dataStore.GetDeviceByCodeAsync(Arg.Any<string>()).Returns(new Device() { Code = "taken" }, (Device?)null);
            dataStore.CreateNewDevice(Arg.Any<string>()).Returns(ci => new Device() { Code = ci.Arg<string>() });

            var device = await deviceService.RegisterNewDeviceAsync();

            Assert.That(device.Code, Does.Match(@"^\d{6}$"));
            await dataStore.Received(2).GetDeviceByCodeAsync(Arg.Any<string>());
            await dataStore.Received(1).CreateNewDevice(device.Code);
        }

        [Test]
        public void GetDevicesForFixtureRequiresReadAccess()
        {
            Assert.ThrowsAsync<AccessDeniedException>(() => deviceService.GetDevicesForFixtureAsync(FixtureId, User));
        }

        [Test]
        public async Task GetDevicesForFixtureReturnsDevices()
        {
            var devices = new[] { new Device() { Id = 1, Code = "111111", FixtureId = FixtureId } };
            dataStore.CheckReadAccessToFixtureAsync(FixtureId, User).Returns(true);
            dataStore.GetDevicesForFixtureAsync(FixtureId).Returns(devices);

            Assert.That(await deviceService.GetDevicesForFixtureAsync(FixtureId, User), Is.EqualTo(devices));
        }

        [Test]
        public void GetDeviceForFixtureRejectsDeviceOfOtherFixture()
        {
            dataStore.CheckReadAccessToFixtureAsync(FixtureId, User).Returns(true);
            dataStore.GetDeviceByIdAsync(7).Returns(new Device() { Id = 7, Code = "777777", FixtureId = 99 });

            Assert.ThrowsAsync<NotFoundException>(() => deviceService.GetDeviceForFixtureAsync(FixtureId, 7, User));
        }

        [Test]
        public void AssignDeviceRequiresWriteAccess()
        {
            Assert.ThrowsAsync<AccessDeniedException>(() => deviceService.AssignDeviceToFixtureAsync(FixtureId, "123456", User));
        }

        [Test]
        public void AssignDeviceFailsForUnknownCode()
        {
            dataStore.CheckWriteAccessToFixtureAsync(FixtureId, User).Returns(true);

            Assert.ThrowsAsync<NotFoundException>(() => deviceService.AssignDeviceToFixtureAsync(FixtureId, "123456", User));
        }

        [Test]
        public void AssignDeviceFailsWhenAssignedToOtherFixture()
        {
            dataStore.CheckWriteAccessToFixtureAsync(FixtureId, User).Returns(true);
            dataStore.GetDeviceByCodeAsync("123456").Returns(new Device() { Code = "123456", FixtureId = 99 });

            Assert.ThrowsAsync<ConflictException>(() => deviceService.AssignDeviceToFixtureAsync(FixtureId, "123456", User));
        }

        [Test]
        public async Task AssignDeviceSetsFixture()
        {
            var device = new Device() { Code = "123456" };
            dataStore.CheckWriteAccessToFixtureAsync(FixtureId, User).Returns(true);
            dataStore.GetDeviceByCodeAsync("123456").Returns(device);

            var result = await deviceService.AssignDeviceToFixtureAsync(FixtureId, "123456", User);

            Assert.That(result.FixtureId, Is.EqualTo(FixtureId));
            await dataStore.Received(1).UpdateDeviceAsync(device);

            Assert.That(logger.LatestRecord.Level, Is.EqualTo(LogLevel.Information));
            Assert.That(logger.LatestRecord.Message, Does.Contain($"assigned device 0 (123456) to fixture {FixtureId}"));
        }

        [Test]
        public void RemoveDeviceFailsWhenNotAssignedToFixture()
        {
            dataStore.CheckWriteAccessToFixtureAsync(FixtureId, User).Returns(true);
            dataStore.GetDeviceByIdAsync(7).Returns(new Device() { Id = 7, Code = "777777", FixtureId = 99 });

            Assert.ThrowsAsync<NotFoundException>(() => deviceService.RemoveDeviceFromFixtureAsync(FixtureId, 7, User));
        }

        [Test]
        public async Task RemoveDeviceClearsFixture()
        {
            var device = new Device() { Id = 7, Code = "777777", FixtureId = FixtureId };
            dataStore.CheckWriteAccessToFixtureAsync(FixtureId, User).Returns(true);
            dataStore.GetDeviceByIdAsync(7).Returns(device);

            await deviceService.RemoveDeviceFromFixtureAsync(FixtureId, 7, User);

            Assert.That(device.FixtureId, Is.Null);
            await dataStore.Received(1).UpdateDeviceAsync(device);
        }

        [Test]
        public async Task UpdateDeviceAppliesDisplaySettings()
        {
            var device = new Device() { Id = 7, Code = "777777", FixtureId = FixtureId };
            dataStore.CheckWriteAccessToFixtureAsync(FixtureId, User).Returns(true);
            dataStore.GetDeviceByIdAsync(7).Returns(device);

            var result = await deviceService.UpdateDeviceAsync(FixtureId, 7, User, DisplayType.Match, DisplayTheme.Light, 3);

            Assert.That(result.DisplayType, Is.EqualTo(DisplayType.Match));
            Assert.That(result.DisplayTheme, Is.EqualTo(DisplayTheme.Light));
            Assert.That(result.MatchNo, Is.EqualTo(3));
            await dataStore.Received(1).UpdateDeviceAsync(device);
        }

        [Test]
        public async Task UpdateDeviceRequiresMatchNoForMatchDisplay()
        {
            var device = new Device() { Id = 7, Code = "777777", FixtureId = FixtureId };
            dataStore.CheckWriteAccessToFixtureAsync(FixtureId, User).Returns(true);
            dataStore.GetDeviceByIdAsync(7).Returns(device);

            var exception = Assert.ThrowsAsync<InvalidRequestException>(() => deviceService.UpdateDeviceAsync(FixtureId, 7, User, DisplayType.Match, DisplayTheme.Light, null));

            Assert.That(exception!.Code, Is.EqualTo("MATCH_NO_REQUIRED"));
            await dataStore.DidNotReceiveWithAnyArgs().UpdateDeviceAsync(default!);
        }

        [TestCase(0)]
        [TestCase(5)]
        [TestCase(99)]
        public async Task UpdateDeviceRejectsMatchNoOutsideMatchPlayChart(int matchNo)
        {
            var device = new Device() { Id = 7, Code = "777777", FixtureId = FixtureId };
            dataStore.CheckWriteAccessToFixtureAsync(FixtureId, User).Returns(true);
            dataStore.GetDeviceByIdAsync(7).Returns(device);
            dataStore.GetMatchCountAsync(FixtureId).Returns(4);

            var exception = Assert.ThrowsAsync<InvalidRequestException>(() => deviceService.UpdateDeviceAsync(FixtureId, 7, User, DisplayType.Match, DisplayTheme.Light, matchNo));

            Assert.That(exception!.Code, Is.EqualTo("INVALID_MATCH_NO"));
            await dataStore.DidNotReceiveWithAnyArgs().UpdateDeviceAsync(default!);
        }

        [Test]
        public async Task UpdateDeviceAcceptsLastMatch()
        {
            var device = new Device() { Id = 7, Code = "777777", FixtureId = FixtureId };
            dataStore.CheckWriteAccessToFixtureAsync(FixtureId, User).Returns(true);
            dataStore.GetDeviceByIdAsync(7).Returns(device);
            dataStore.GetMatchCountAsync(FixtureId).Returns(4);

            var result = await deviceService.UpdateDeviceAsync(FixtureId, 7, User, DisplayType.Match, DisplayTheme.Dark, 4);

            Assert.That(result.MatchNo, Is.EqualTo(4));
            await dataStore.Received(1).UpdateDeviceAsync(device);
        }

        [Test]
        public async Task UpdateDeviceAllowsTableWithoutMatchNo()
        {
            var device = new Device() { Id = 7, Code = "777777", FixtureId = FixtureId };
            dataStore.CheckWriteAccessToFixtureAsync(FixtureId, User).Returns(true);
            dataStore.GetDeviceByIdAsync(7).Returns(device);

            await deviceService.UpdateDeviceAsync(FixtureId, 7, User, DisplayType.Table, DisplayTheme.Dark, null);

            await dataStore.Received(1).UpdateDeviceAsync(device);
        }
    }
}
