using GaldrJson;
using System.Collections.Generic;

namespace AudibleDownloader.Models.Audible;

internal class LibraryItem
{
    [GaldrJsonPropertyName("asin")]
    public string Asin { get; set; }

    [GaldrJsonPropertyName("title")]
    public string Title { get; set; }

    [GaldrJsonPropertyName("subtitle")]
    public string Subtitle { get; set; }

    [GaldrJsonPropertyName("authors")]
    public Author[] Authors { get; set; }

    [GaldrJsonPropertyName("narrators")]
    public Narrator[] Narrators { get; set; }

    [GaldrJsonPropertyName("relationships")]
    public Relationship[] Relationships { get; set; }

    [GaldrJsonPropertyName("runtime_length_min")]
    public int RuntimeLengthMin { get; set; }

    [GaldrJsonPropertyName("purchase_date")]
    public string PurchaseDate { get; set; }

    [GaldrJsonPropertyName("release_date")]
    public string ReleaseDate { get; set; }

    [GaldrJsonPropertyName("product_images")]
    public Dictionary<string, string> ProductImages { get; set; }

    [GaldrJsonPropertyName("available_codecs")]
    public AvailableCodec[] AvailableCodecs { get; set; }

    [GaldrJsonPropertyName("merchandising_summary")]
    public string MerchandisingSummary { get; set; }

    [GaldrJsonPropertyName("is_playable")]
    public bool IsPlayable { get; set; }

    [GaldrJsonPropertyName("series")]
    public Series[] Series { get; set; }
}
