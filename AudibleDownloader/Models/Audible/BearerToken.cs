using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;

internal class BearerToken
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }

    [JsonPropertyName("expires_in")]
    public string ExpiresIn { get; set; }
}
