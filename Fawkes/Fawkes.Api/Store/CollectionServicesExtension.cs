using Microsoft.EntityFrameworkCore;

namespace Fawkes.Api.Store
{
    public static class CollectionServicesExtension
    {
        extension(IServiceCollection services)
        {
            /// <summary>
            /// Injects Fawkes data layer services into the provided IServiceCollection.
            /// </summary>
            /// <param name="services"></param>
            public void AddFawkesDataLayer(IConfiguration configuration)
            {
                services.AddDbContextPool<FawkesDbContext>(options =>
                {
                    options.UseNpgsql(configuration.GetConnectionString("FawkesConnection"));
                });




                services.AddScoped<IFawkesDataStore, FawkesDataStore>();
            }
        }
    }
}
