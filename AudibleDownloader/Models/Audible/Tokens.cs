using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;

internal class Tokens
{
    [JsonPropertyName("mac_dms")]
    public MacDmsToken MacDms { get; set; }

    [JsonPropertyName("bearer")]
    public BearerToken Bearer { get; set; }

    [JsonPropertyName("website_cookies")]
    public WebsiteCookie[] WebsiteCookies { get; set; }

    [JsonPropertyName("store_authentication_cookie")]
    public StoreAuthenticationCookie StoreAuthenticationCookie { get; set; }
}
