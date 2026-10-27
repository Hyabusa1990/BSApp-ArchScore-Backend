using Fawkes.Core.Exceptions;
using Fawkes.Core.Model;
using Fawkes.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Fawkes.Core.Utils;

namespace Fawkes.Core.Services
{



    public interface IDeviceService
    {
        Task<IEnumerable<Device>> GetDevicesForFixtureAsync(int fixtureId, string userName);
        Task<Device> GetDeviceForFixtureAsync(int fixtureId, int deviceId, string userName);
        Task<Device> RegisterNewDeviceAsync();
        Task<Device> AssignDeviceToFixtureAsync(int fixtureId, string deviceCode, string userName);
        Task RemoveDeviceFromFixtureAsync(int fixtureId, int deviceId, string userName);
        Task<Device> UpdateDeviceAsync(int fixtureId, int deviceId, string userName, DisplayType displayType, DisplayTheme displayTheme, int? matchNo);
    }


    public class DeviceService(IFawkesDataStore fawkesDataStore, RandomService randomService, ILogger<DeviceService> logger) : IDeviceService
    {
        private static readonly string DEVICE_CODE_CHARS = "0123456789";
        private static readonly int DEVICE_CODE_LENGTH = 6;


        public async Task<Device> RegisterNewDeviceAsync()
        {
            var deviceCode = string.Empty;
            do
            {
                deviceCode = GenerateDeviceCode();
            }
            while (await fawkesDataStore.GetDeviceByCodeAsync(deviceCode) != null);

            var newDevice = await fawkesDataStore.CreateNewDevice(deviceCode);

            logger.LogInformation("Registered new display device {DeviceId} with code {DeviceCode}", newDevice.Id, newDevice.Code);
            return newDevice;
        }

        private string GenerateDeviceCode()
        {
            var deviceCode = new char[DEVICE_CODE_LENGTH];
            for (int i = 0; i < DEVICE_CODE_LENGTH; i++)
            {
                deviceCode[i] = DEVICE_CODE_CHARS[randomService.Next(DEVICE_CODE_CHARS.Length)];
            }
            return new string(deviceCode);
        }

        public async Task<IEnumerable<Device>> GetDevicesForFixtureAsync(int fixtureId, string userName)
        {
            await fawkesDataStore.EnsureReadAccessAsync(fixtureId, userName);
            return await fawkesDataStore.GetDevicesForFixtureAsync(fixtureId);
        }

        public async Task<Device> GetDeviceForFixtureAsync(int fixtureId, int deviceId, string userName)
        {
            await fawkesDataStore.EnsureReadAccessAsync(fixtureId, userName);
            var device = await fawkesDataStore.GetDeviceByIdAsync(deviceId);
            if (device == null || device.FixtureId != fixtureId)
            {
                throw new NotFoundException($"Device with ID '{deviceId}' not found for fixture '{fixtureId}'.");
            }
            return device;
        }

        public async Task<Device> AssignDeviceToFixtureAsync(int fixtureId, string deviceCode, string userName)
        {
            await fawkesDataStore.EnsureWriteAccessAsync(fixtureId, userName);

            var device = await fawkesDataStore.GetDeviceByCodeAsync(deviceCode);

            if (device == null)
            {
                throw new NotFoundException($"Device with code '{deviceCode}' not found.");
            }

            if (device.FixtureId != null && device.FixtureId.Value != fixtureId)
            {
                throw new ConflictException($"Device with code '{deviceCode}' is already assigned to fixture '{device.FixtureId}'.", "DEVICE_ASSIGNED_ELSEWHERE");
            }

            device.FixtureId = fixtureId;

            await fawkesDataStore.UpdateDeviceAsync(device);
            logger.LogInformation("User {UserName} assigned device {DeviceId} ({DeviceCode}) to fixture {FixtureId}", userName, device.Id, device.Code, fixtureId);

            return device;
        }

        public async Task RemoveDeviceFromFixtureAsync(int fixtureId, int deviceId, string userName)
        {
            await fawkesDataStore.EnsureWriteAccessAsync(fixtureId, userName);

            var device = await fawkesDataStore.GetDeviceByIdAsync(deviceId);

            if (device == null)
            {
                throw new NotFoundException($"Device with ID '{deviceId}' not found.");
            }

            if (device.FixtureId != fixtureId)
            {
                throw new NotFoundException($"Device with ID '{deviceId}' is not assigned to fixture '{fixtureId}'.");
            }

            device.FixtureId = null;

            await fawkesDataStore.UpdateDeviceAsync(device);
            logger.LogInformation("User {UserName} removed device {DeviceId} from fixture {FixtureId}", userName, deviceId, fixtureId);
        }

        public async Task<Device> UpdateDeviceAsync(int fixtureId, int deviceId, string userName, DisplayType displayType, DisplayTheme displayTheme, int? matchNo)
        {
            await fawkesDataStore.EnsureWriteAccessAsync(fixtureId, userName);

            var device = await fawkesDataStore.GetDeviceByIdAsync(deviceId);

            if (device == null)
            {
                throw new NotFoundException($"Device with ID '{deviceId}' not found.");
            }

            if (device.FixtureId != fixtureId)
            {
                throw new NotFoundException($"Device with ID '{deviceId}' is not assigned to fixture '{fixtureId}'.");
            }

            if (displayType == DisplayType.Match)
            {
                if (matchNo == null)
                    throw new InvalidRequestException("A match display needs a match number.", "MATCH_NO_REQUIRED");

                // Before the match play chart exists the number of matches is unknown, so only the lower bound applies.
                var matchCount = await fawkesDataStore.GetMatchCountAsync(fixtureId);
                if (matchNo < 1 || (matchCount > 0 && matchNo > matchCount))
                    throw new InvalidRequestException($"Match number must be between 1 and {matchCount} (was {matchNo}).", "INVALID_MATCH_NO");
            }

            device.DisplayType = displayType;
            device.DisplayTheme = displayTheme;
            device.MatchNo = matchNo;


            await fawkesDataStore.UpdateDeviceAsync(device);
            logger.LogInformation("User {UserName} set device {DeviceId} of fixture {FixtureId} to {DisplayType}/{DisplayTheme}, match {MatchNo}", userName, deviceId, fixtureId, displayType, displayTheme, matchNo);

            return device;
        }
    }



}
