using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;

internal class LibraryResponse
{
    [JsonPropertyName("items")]
    public LibraryItem[] Items { get; set; }
}
