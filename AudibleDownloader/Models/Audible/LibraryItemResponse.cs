using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;

internal class LibraryItemResponse
{
    [JsonPropertyName("item")]
    public LibraryItem Item { get; set; }
}
