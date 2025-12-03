using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;

internal class DeregisterResponse
{
    [JsonPropertyName("response")]
    public DeregisterResponseWrapper Response { get; set; }
}
