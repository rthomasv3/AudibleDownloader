using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;

internal class DeregisterResponseWrapper
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
}
