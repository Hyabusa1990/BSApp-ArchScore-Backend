using Fawkes.Api.Controllers;
using Fawkes.Api.Store;
using Fawkes.Api.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

namespace Fawkes.Api.Core
{


    public interface IDeviceService
    {
        Task<Device> RegisterNewDeviceAsync();
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
    }




    public class Device
    {
        public int Id { get; set; }
        public int? FixtureId { get; set; }
        public string Code { get; set; }
        public DisplayType DisplayType { get; set; }
        public DisplayTheme DisplayTheme { get; set; }
     }


    public enum DisplayType
    {
        None,
        Match,
        Table
    }



}
