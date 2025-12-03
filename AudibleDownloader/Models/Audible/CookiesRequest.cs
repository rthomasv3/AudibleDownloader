using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;


internal class CookiesRequest
{
    [JsonPropertyName("website_cookies")]
    public object[] WebsiteCookies { get; set; }

    [JsonPropertyName("domain")]
    public string Domain { get; set; }
}
