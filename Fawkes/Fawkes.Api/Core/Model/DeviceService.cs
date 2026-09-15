using Fawkes.Api.Controllers;
using Fawkes.Api.Core.Services;
using Fawkes.Api.Store;
using Fawkes.Api.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

namespace Fawkes.Api.Core.Model
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


    public class DeviceService(IFawkesDataStore fawkesDataStore, RandomService randomService) : IDeviceService
    {
        private static readonly string DEVICE_CODE_CHARS = "0123456789";
        private static readonly int DEVICE_CODE_LENGTH = 6;
        private static readonly log4net.ILog log
            = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public async Task<Device> RegisterNewDeviceAsync()
        {
            var deviceCode = string.Empty;
            do
            {
                deviceCode = GenerateDeviceCode();
            }
            while (await fawkesDataStore.GetDeviceByCodeAsync(deviceCode) != null);

            var newDevice = await fawkesDataStore.CreateNewDevice(deviceCode);

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
            if (!await fawkesDataStore.CheckReadAccessToFixtureAsync(fixtureId, userName))
            {
                throw new UnauthorizedAccessException($"User '{userName}' does not have read access to fixture '{fixtureId}'.");
            }
            return await fawkesDataStore.GetDevicesForFixtureAsync(fixtureId);
        }

        public async Task<Device> GetDeviceForFixtureAsync(int fixtureId, int deviceId, string userName)
        {
            if (!await fawkesDataStore.CheckReadAccessToFixtureAsync(fixtureId, userName))
            {
                throw new UnauthorizedAccessException($"User '{userName}' does not have read access to fixture '{fixtureId}'.");
            }
            var device = await fawkesDataStore.GetDeviceByIdAsync(deviceId);
            if (device == null || device.FixtureId != fixtureId)
            {
                throw new KeyNotFoundException($"Device with ID '{deviceId}' not found for fixture '{fixtureId}'.");
            }
            return device;
        }

        public async Task<Device> AssignDeviceToFixtureAsync(int fixtureId, string deviceCode, string userName)
        {
            if (!await fawkesDataStore.CheckWriteAccessToFixtureAsync(fixtureId, userName))
            {
                throw new UnauthorizedAccessException($"User '{userName}' does not have write access to fixture '{fixtureId}'.");
            }

            var device = await fawkesDataStore.GetDeviceByCodeAsync(deviceCode);

            if (device == null)
            {
                throw new KeyNotFoundException($"Device with code '{deviceCode}' not found.");
            }

            if (device.FixtureId != null && device.FixtureId.Value != fixtureId)
            {
                throw new InvalidOperationException($"Device with code '{deviceCode}' is already assigned to fixture '{device.FixtureId}'.");
            }

            device.FixtureId = fixtureId;

            await fawkesDataStore.UpdateDeviceAsync(device);

            return device;
        }

        public async Task RemoveDeviceFromFixtureAsync(int fixtureId, int deviceId, string userName)
        {
            if (!await fawkesDataStore.CheckWriteAccessToFixtureAsync(fixtureId, userName))
            {
                throw new UnauthorizedAccessException($"User '{userName}' does not have write access to fixture '{fixtureId}'.");
            }

            var device = await fawkesDataStore.GetDeviceByIdAsync(deviceId);

            if (device == null)
            {
                throw new KeyNotFoundException($"Device with ID '{deviceId}' not found.");
            }

            if (device.FixtureId != fixtureId)
            {
                throw new InvalidOperationException($"Device with ID '{deviceId}' is not assigned to fixture '{fixtureId}'.");
            }

            device.FixtureId = null;

            await fawkesDataStore.UpdateDeviceAsync(device);
        }

        public async Task<Device> UpdateDeviceAsync(int fixtureId, int deviceId, string userName, DisplayType displayType, DisplayTheme displayTheme, int? matchNo)
        {
            if (!await fawkesDataStore.CheckWriteAccessToFixtureAsync(fixtureId, userName))
            {
                throw new UnauthorizedAccessException($"User '{userName}' does not have write access to fixture '{fixtureId}'.");
            }

            var device = await fawkesDataStore.GetDeviceByIdAsync(deviceId);

            if (device == null)
            {
                throw new KeyNotFoundException($"Device with ID '{deviceId}' not found.");
            }

            if (device.FixtureId != fixtureId)
            {
                throw new InvalidOperationException($"Device with ID '{deviceId}' is not assigned to fixture '{fixtureId}'.");
            }

            device.DisplayType = displayType;
            device.DisplayTheme = displayTheme;
            device.MatchNo = matchNo;


            await fawkesDataStore.UpdateDeviceAsync(device);

            return device;
        }
    }



}
