using GaldrJson;

namespace AudibleDownloader.Models.Audible;

internal class Author
{
    [GaldrJsonPropertyName("asin")]
    public string Asin { get; set; }

    [GaldrJsonPropertyName("name")]
    public string Name { get; set; }
}
