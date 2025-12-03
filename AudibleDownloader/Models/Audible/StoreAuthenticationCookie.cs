using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;

internal class StoreAuthenticationCookie
{
    [JsonPropertyName("cookie")]
    public string Cookie { get; set; }
}
