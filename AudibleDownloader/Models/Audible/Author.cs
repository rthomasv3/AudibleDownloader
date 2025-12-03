using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;

internal class Author
{
    [JsonPropertyName("asin")]
    public string Asin { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}
