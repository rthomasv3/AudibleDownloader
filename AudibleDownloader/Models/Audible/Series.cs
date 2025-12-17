using GaldrJson;

namespace AudibleDownloader.Models.Audible;

internal class Series
{
    [GaldrJsonPropertyName("asin")]
    public string Asin { get; set; }

    [GaldrJsonPropertyName("sequence")]
    public string Sequence { get; set; }

    [GaldrJsonPropertyName("title")]
    public string Title { get; set; }
}
