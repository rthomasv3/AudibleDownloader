using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;

internal class AvailableCodec
{
    [JsonPropertyName("enhanced_codec")]
    public string EnhancedCodec { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("format")]
    public string Format { get; set; }
}
