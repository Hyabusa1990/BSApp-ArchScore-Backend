using Fawkes.Api.Core.Model;
using Fawkes.Api.Core.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Fawkes.Api.Core
{
    public static class CollectionServicesExtension
    {
        extension(IServiceCollection services)
        {
            /// <summary>
            /// Injects application logic services into the provided IServiceCollection.
            /// </summary>
            public void AddApplicationLogic()
            {
                services.AddTransient<IDeviceService, DeviceService>();
                services.AddTransient<IDisplayService, DisplayService>();
                services.AddTransient<IFixtureService, FixtureService>();
                services.AddTransient<IMatchPlayChartService, MatchPlayChartService>();
            }
        }
    }

}
