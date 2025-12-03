using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;

internal class WebsiteCookie
{
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    [JsonPropertyName("Value")]
    public string Value { get; set; }
}
