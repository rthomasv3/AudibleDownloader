using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;

internal class ProductImages
{
    [JsonPropertyName("500")]
    public string Image500 { get; set; }
}
