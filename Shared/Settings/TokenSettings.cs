using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Options;

namespace Shared.Settings
{
    public class TokenSettings(
        IOptions<AdSettings> adSettings, 
        IOptions<UserCredentials> userCredentials)
    {
        private readonly AdSettings _adSettings = adSettings.Value;
        private readonly UserCredentials _userCredentials = userCredentials.Value;

        public Uri TokenUri => new(new Uri($"{_adSettings.Instance}"), $"/{_adSettings.TenantId}/oauth2/v2.0/token");

        public FormUrlEncodedContent AccessTokenRequestContent
        {
            get
            {
                var content = new List<KeyValuePair<string, string>>()
                {
                    new("client_id", _adSettings.ClientId),
                    new("scope", _adSettings.Scope),
                    new("client_secret", _adSettings.ClientSecret),
                    new("grant_type", _adSettings.GrantType),
                    new("username", _userCredentials.Username),
                    new("password", _userCredentials.Password),
                };
                return new FormUrlEncodedContent(content);
            }
        }
    }
}
