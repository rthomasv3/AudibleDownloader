using GaldrJson;

namespace AudibleDownloader.Models.Audible;

internal class Tokens
{
    [GaldrJsonPropertyName("mac_dms")]
    public MacDmsToken MacDms { get; set; }

    [GaldrJsonPropertyName("bearer")]
    public BearerToken Bearer { get; set; }

    [GaldrJsonPropertyName("website_cookies")]
    public WebsiteCookie[] WebsiteCookies { get; set; }

    [GaldrJsonPropertyName("store_authentication_cookie")]
    public StoreAuthenticationCookie StoreAuthenticationCookie { get; set; }
}
