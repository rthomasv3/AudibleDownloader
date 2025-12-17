using GaldrJson;

namespace AudibleDownloader.Models.Audible;

internal class Relationship
{
    [GaldrJsonPropertyName("asin")]
    public string Asin { get; set; }

    [GaldrJsonPropertyName("relationship_type")]
    public string RelationshipType { get; set; }

    [GaldrJsonPropertyName("relationship_to_product")]
    public string RelationshipToProduct { get; set; }

    [GaldrJsonPropertyName("sort")]
    public string Sort { get; set; }
}
