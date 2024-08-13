using ChangeNotification.Service.Certificate;
using ChangeNotification.Service.Queue;
using Shared;
using Shared.Settings;

namespace ChangeNotification.Service
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddMemoryCache();

            services.AddGraphService();

            services.AddSingleton<SubscriptionStore>();
            services.AddSingleton<CertificateService>();
            services.AddScoped<ServiceBusSender>();

            return services;
        }

        public static IServiceCollection AddSettings(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AdSettings>(configuration.GetSection("AdSettings"));
            services.Configure<UserCredentials>(configuration.GetSection("UserCredentials"));
            services.AddSingleton<TokenSettings>();
            services.Configure<ServiceBusSettings>(configuration.GetSection("ServiceBus"));
            
            return services;
        }
    }
}
