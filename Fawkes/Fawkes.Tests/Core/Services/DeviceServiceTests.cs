using Fawkes.Core.Model;
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

        [SetUp]
        public void Setup()
        {
            dataStore = Substitute.For<IFawkesDataStore>();
            deviceService = new DeviceService(dataStore, new RandomService(42));
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
            Assert.ThrowsAsync<UnauthorizedAccessException>(() => deviceService.GetDevicesForFixtureAsync(FixtureId, User));
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

            Assert.ThrowsAsync<KeyNotFoundException>(() => deviceService.GetDeviceForFixtureAsync(FixtureId, 7, User));
        }

        [Test]
        public void AssignDeviceRequiresWriteAccess()
        {
            Assert.ThrowsAsync<UnauthorizedAccessException>(() => deviceService.AssignDeviceToFixtureAsync(FixtureId, "123456", User));
        }

        [Test]
        public void AssignDeviceFailsForUnknownCode()
        {
            dataStore.CheckWriteAccessToFixtureAsync(FixtureId, User).Returns(true);

            Assert.ThrowsAsync<KeyNotFoundException>(() => deviceService.AssignDeviceToFixtureAsync(FixtureId, "123456", User));
        }

        [Test]
        public void AssignDeviceFailsWhenAssignedToOtherFixture()
        {
            dataStore.CheckWriteAccessToFixtureAsync(FixtureId, User).Returns(true);
            dataStore.GetDeviceByCodeAsync("123456").Returns(new Device() { Code = "123456", FixtureId = 99 });

            Assert.ThrowsAsync<InvalidOperationException>(() => deviceService.AssignDeviceToFixtureAsync(FixtureId, "123456", User));
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
        }

        [Test]
        public void RemoveDeviceFailsWhenNotAssignedToFixture()
        {
            dataStore.CheckWriteAccessToFixtureAsync(FixtureId, User).Returns(true);
            dataStore.GetDeviceByIdAsync(7).Returns(new Device() { Id = 7, Code = "777777", FixtureId = 99 });

            Assert.ThrowsAsync<InvalidOperationException>(() => deviceService.RemoveDeviceFromFixtureAsync(FixtureId, 7, User));
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
    }
}
