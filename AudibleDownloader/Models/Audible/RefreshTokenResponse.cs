using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;

internal class RefreshTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}
