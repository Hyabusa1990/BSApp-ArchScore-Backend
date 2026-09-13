namespace Fawkes.Api.Utils
{
    public static class CollectionServicesExtension
    {
        extension(IServiceCollection services)
        {
            /// <summary>
            /// Injects utility services into the provided IServiceCollection.
            /// </summary>
            /// <param name="services"></param>
            public void AddUtils()
            {
                services.AddSingleton<EmailService>();
                services.AddSingleton<RandomService>();
            }
        }
    }
}
