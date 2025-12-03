using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;

internal class SuccessResponse
{
    [JsonPropertyName("tokens")]
    public Tokens Tokens { get; set; }

    [JsonPropertyName("extensions")]
    public Extensions Extensions { get; set; }
}
