using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Services.Authentication
{
    public class AuthenticationProvider(IAuthenticationClient tokenProvider) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var jwtToken = await tokenProvider.GetAccessTokenAsync();
            if (jwtToken != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken.AccessToken);
                return await base.SendAsync(request, cancellationToken);
            }

            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }
    }
}
