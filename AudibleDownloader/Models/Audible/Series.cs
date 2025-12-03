using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;

internal class Series
{
    [JsonPropertyName("asin")]
    public string Asin { get; set; }

    [JsonPropertyName("sequence")]
    public string Sequence { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }
}
