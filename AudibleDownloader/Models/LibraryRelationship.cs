namespace AudibleDownloader.Models;

internal class LibraryRelationship
{
    public string Asin { get; set; }
    public string RelationshipType { get; set; }
    public string RelationshipToProduct { get; set; }
    public int Sort { get; set; }
}
