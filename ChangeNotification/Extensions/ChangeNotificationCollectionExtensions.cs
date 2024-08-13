using System.IdentityModel.Tokens.Jwt;
using Microsoft.Graph.Models;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Shared.Settings;

namespace ChangeNotification.Extensions
{
    public static class ChangeNotificationCollectionExtensions
    {
        /// <summary>
        /// Validates all tokens contained in a ChangeNotificationCollection. If there are none, returns true.
        /// </summary>
        /// <param name="collection">The ChangeNotificationCollection to validate</param>
        /// <param name="adSettings"></param>
        /// <returns>true if all tokens are valid, false otherwise</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<bool> AreTokensValid(
            this ChangeNotificationCollection collection,
            AdSettings adSettings)
        {
            var wellKnownUri = new Uri(new Uri(adSettings.Instance), $"{adSettings.TenantId}/v2.0/.well-known/openid-configuration");
            if ((collection.ValidationTokens == null || !collection.ValidationTokens.Any()) &&
                (collection.Value == null || collection.Value.All(x => x.EncryptedContent == null)))
                return true;

            if (adSettings.TenantId.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(adSettings.TenantId));
            if (adSettings.ClientId.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(adSettings.ClientId));

            var issuerFormats = new[]
            {
                "https://sts.windows.net/{0}/",
                "https://login.microsoftonline.com/{0}/v2.0"
            };

            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                wellKnownUri.AbsoluteUri, new OpenIdConnectConfigurationRetriever());

            var openIdConfig = await configurationManager.GetConfigurationAsync();
            var handler = new JwtSecurityTokenHandler();

            foreach (var issuerFormat in issuerFormats)
            {
                var issuersToValidate = string.Format(issuerFormat, adSettings.TenantId);
                if (collection.ValidationTokens != null)
                {
                    var result = collection.ValidationTokens
                        .Select(t => IsTokenValid(t, handler, openIdConfig, issuersToValidate, adSettings.ClientId))
                        .Aggregate((x, y) => x && y);

                    if (result) return result;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a given token is valid
        /// </summary>
        /// <param name="token">The token to validate</param>
        /// <param name="handler">The JwtSecurityTokenHandler to use to validate the token</param>
        /// <param name="openIdConnectConfiguration">OpenID configuration information</param>
        /// <param name="issuerToValidate">A valid issuer</param>
        /// <param name="audience">A valid audience</param>
        /// <returns>true if token is valid, false if not</returns>
        private static bool IsTokenValid(
            string token,
            JwtSecurityTokenHandler handler,
            OpenIdConnectConfiguration openIdConnectConfiguration,
            string issuerToValidate,
            string audience)
        {
            try
            {
                handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuers = new[] { issuerToValidate },
                    ValidAudiences = new[] { audience },
                    IssuerSigningKeys = openIdConnectConfiguration.SigningKeys
                }, out _);

                return true;
            }
            catch (SecurityTokenValidationException)
            {
                return false;
            }
        }
    }
}
