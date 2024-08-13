using Shared.Services.Authentication;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;

namespace Shared
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGraphService(this IServiceCollection services)
        {
            services.AddHttpClient(nameof(AuthenticationClient));
            services.AddScoped<IAuthenticationClient, AuthenticationClient>();
            services.AddScoped<AuthenticationProvider>();

            services.AddScoped(sp =>
            {
                var delegatedAuthentication = sp.GetRequiredService<IAuthenticationClient>();

                var httpClient = GraphClientFactory.Create(new DelegatingHandler[]
                {
                    new AuthenticationProvider(delegatedAuthentication)
                });
                return new GraphServiceClient(httpClient);
            });
        }
    }
}
