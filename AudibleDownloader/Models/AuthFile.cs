using System.Collections.Generic;
using System.Text.Json.Serialization;
using AudibleDownloader.Models.Audible;

namespace AudibleDownloader.Models;

internal class AuthFile
{
    [JsonPropertyName("website_cookies")]
    public Dictionary<string, string> WebsiteCookies { get; set; }

    [JsonPropertyName("adp_token")]
    public string AdpToken { get; set; }

    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }

    [JsonPropertyName("device_private_key")]
    public string DevicePrivateKey { get; set; }

    [JsonPropertyName("store_authentication_cookie")]
    public StoreAuthenticationCookie StoreAuthenticationCookie { get; set; }

    [JsonPropertyName("device_info")]
    public DeviceInfo DeviceInfo { get; set; }

    [JsonPropertyName("customer_info")]
    public CustomerInfo CustomerInfo { get; set; }

    [JsonPropertyName("expires")]
    public double Expires { get; set; }

    [JsonPropertyName("locale_code")]
    public string LocaleCode { get; set; }

    [JsonPropertyName("with_username")]
    public bool WithUsername { get; set; }

    [JsonPropertyName("activation_bytes")]
    public string ActivationBytes { get; set; }
}
