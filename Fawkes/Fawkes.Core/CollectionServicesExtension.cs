using Fawkes.Core.Rules;
using Fawkes.Core.Services;
using Fawkes.Core.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Fawkes.Core
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
                services.AddSingleton<RandomService>();

                services.AddTransient<IDeviceService, DeviceService>();
                services.AddTransient<IDisplayService, DisplayService>();
                services.AddTransient<IFixtureService, FixtureService>();
                services.AddTransient<ILeagueTableService, LeagueTableService>();
                services.AddTransient<IMatchPlayChartService, MatchPlayChartService>();
                services.AddTransient<IDosService, DosService>();
                services.AddTransient<ISpotterService, SpotterService>();
                services.AddTransient<IRuleSetFactory, RuleSetFactory>();

            }
        }
    }

}
