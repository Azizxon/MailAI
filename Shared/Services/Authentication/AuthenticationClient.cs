using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shared.Models;
using Shared.Settings;

namespace Shared.Services.Authentication
{
    public class AuthenticationClient(
        IHttpClientFactory httpClientFactory,
        TokenSettings settings,
        ILogger<AuthenticationClient> logger) : IAuthenticationClient
    {
        public async Task<Token?> GetAccessTokenAsync()
        {
            var tokenRequest = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = settings.TokenUri,
                Content = settings.AccessTokenRequestContent
            };
            return await SendRequestAsync(tokenRequest);
        }

        public async Task<Token?> SendRequestAsync(HttpRequestMessage request)
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient(nameof(AuthenticationClient));
                var response = await httpClient.SendAsync(request);
                var responseRawString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Request failed: {response.StatusCode}; Content: {responseRawString}");
                }

                var token = JsonConvert.DeserializeObject<Token>(responseRawString);
                if (token != null)
                {
                    return token;
                }
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
            }

            return default;
        }
    }
}
