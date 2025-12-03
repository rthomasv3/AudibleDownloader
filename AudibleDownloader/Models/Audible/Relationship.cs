using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;

internal class Relationship
{
    [JsonPropertyName("asin")]
    public string Asin { get; set; }

    [JsonPropertyName("relationship_type")]
    public string RelationshipType { get; set; }

    [JsonPropertyName("relationship_to_product")]
    public string RelationshipToProduct { get; set; }

    [JsonPropertyName("sort")]
    public string Sort { get; set; }
}
