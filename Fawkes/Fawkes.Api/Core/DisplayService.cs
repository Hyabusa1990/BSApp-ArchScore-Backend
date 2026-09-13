using Fawkes.Api.Store;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Fawkes.Api.Core
{
    public interface IDisplayService
    {
        Task<DisplayData?> GetDisplayDataAsync(string deviceCode);
    }

    public class DisplayService(IFawkesDataStore dataStore) : IDisplayService
    {
        public async Task<DisplayData?> GetDisplayDataAsync(string deviceCode)
        {
            var device = await dataStore.GetDeviceByCodeAsync(deviceCode);

            if (device == null)
                return null;


            if (device.FixtureId == default)
            {
                return new UnassignedDisplayData()
                {
                    Theme = device.DisplayTheme,
                    DeviceCode = device.Code
                };
            }

            throw new NotImplementedException();

            
        }
    }


    public class DisplayData
    {
        public DisplayTheme Theme { get; set; }
    }

    public class UnassignedDisplayData : DisplayData
    {
        public string DeviceCode { get; set; }
    }

    public enum DisplayTheme
    {
        Dark,
        Light
    }

}
