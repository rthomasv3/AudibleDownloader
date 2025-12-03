using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;

internal class LibraryItem
{
    [JsonPropertyName("asin")]
    public string Asin { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("subtitle")]
    public string Subtitle { get; set; }

    [JsonPropertyName("authors")]
    public Author[] Authors { get; set; }

    [JsonPropertyName("narrators")]
    public Narrator[] Narrators { get; set; }

    [JsonPropertyName("relationships")]
    public Relationship[] Relationships { get; set; }

    [JsonPropertyName("runtime_length_min")]
    public int RuntimeLengthMin { get; set; }

    [JsonPropertyName("purchase_date")]
    public string PurchaseDate { get; set; }

    [JsonPropertyName("release_date")]
    public string ReleaseDate { get; set; }

    [JsonPropertyName("product_images")]
    public Dictionary<string, string> ProductImages { get; set; }

    [JsonPropertyName("available_codecs")]
    public AvailableCodec[] AvailableCodecs { get; set; }

    [JsonPropertyName("merchandising_summary")]
    public string MerchandisingSummary { get; set; }

    [JsonPropertyName("is_playable")]
    public bool IsPlayable { get; set; }

    [JsonPropertyName("series")]
    public Series[] Series { get; set; }
}
