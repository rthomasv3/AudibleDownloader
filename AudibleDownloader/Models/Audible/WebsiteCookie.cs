using GaldrJson;

namespace AudibleDownloader.Models.Audible;

internal class WebsiteCookie
{
    [GaldrJsonPropertyName("Name")]
    public string Name { get; set; }

    [GaldrJsonPropertyName("Value")]
    public string Value { get; set; }
}
