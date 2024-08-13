using Shared;
using Shared.Settings;

namespace MessageProcessor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            AddSettings(builder.Services, builder.Configuration);
            builder.Services.AddGraphService();

            builder.Services.AddScoped<MessageProcessor>();
            builder.Services.AddHostedService<ServiceBusWorker>();
            
            var host = builder.Build();
            host.Run();
        }

        public static IServiceCollection AddSettings(IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AdSettings>(configuration.GetSection("AdSettings"));
            services.Configure<UserCredentials>(configuration.GetSection("UserCredentials"));
            services.AddSingleton<TokenSettings>();
            services.Configure<ServiceBusSettings>(configuration.GetSection("ServiceBus"));

            return services;
        }
    }
}