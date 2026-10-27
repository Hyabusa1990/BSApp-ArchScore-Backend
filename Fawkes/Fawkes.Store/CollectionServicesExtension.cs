using Fawkes.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fawkes.Store
{
    public static class CollectionServicesExtension
    {
        extension(IServiceCollection services)
        {
            /// <summary>
            /// Injects Fawkes data layer services (application and identity database) into the provided IServiceCollection.
            /// </summary>
            public void AddFawkesDataLayer(IConfiguration configuration)
            {
                services.AddDbContextPool<FawkesDbContext>(options =>
                {
                    options.UseNpgsql(configuration.GetConnectionString("FawkesConnection"));
                });

                services.AddDbContextPool<IdentityDbContext>(options =>
                {
                    options.UseNpgsql(configuration.GetConnectionString("FawkesConnection"));
                });

                services.AddScoped<IFawkesDataStore, FawkesDataStore>();
            }
        }
    }
}
