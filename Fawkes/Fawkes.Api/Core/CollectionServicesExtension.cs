using Fawkes.Api.Core.Rules;
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
                services.AddTransient<IDosService, DosService>();
                services.AddTransient<ISpotterService, SpotterService>();
                services.AddTransient<IRuleSetFactory, RuleSetFactory>();

            }
        }
    }

}
