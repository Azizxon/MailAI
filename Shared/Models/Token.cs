using Newtonsoft.Json;

namespace Shared.Models
{
    public class Token
    {
        [JsonProperty("access_token")] public string AccessToken;

        [JsonProperty("expires_in")] public int ExpiresIn;

        [JsonProperty("refresh_expires_in")] public int RefreshExpiresIn;

        [JsonProperty("token_type")] public string TokenType;

        [JsonProperty("not-before-policy")] public int NotBeforePolicy;

        [JsonProperty("scope")] public string Scope;

        [JsonProperty("ext_expires_in")] public string ExtExpiresIn;

        [JsonProperty("expires_on")] public string ExpiresOn;

        [JsonProperty("not_before")] public string NotBefore;

        [JsonProperty("resource")] public string Resource;

        [JsonProperty("refresh_token")] public string RefreshToken;

        [JsonProperty("id_token")] public string IdToken;
    }
}
