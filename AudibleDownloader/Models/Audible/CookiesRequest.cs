using GaldrJson;

namespace AudibleDownloader.Models.Audible;

internal class CookiesRequest
{
    [GaldrJsonPropertyName("website_cookies")]
    public EmptyObject[] WebsiteCookies { get; set; }

    [GaldrJsonPropertyName("domain")]
    public string Domain { get; set; }
}
