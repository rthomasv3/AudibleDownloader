using System.Collections.Generic;
using AudibleDownloader.Models.Audible;
using GaldrJson;

namespace AudibleDownloader.Models;

[GaldrJsonSerializable]
internal class AuthFile
{
    [GaldrJsonPropertyName("website_cookies")]
    public Dictionary<string, string> WebsiteCookies { get; set; }

    [GaldrJsonPropertyName("adp_token")]
    public string AdpToken { get; set; }

    [GaldrJsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [GaldrJsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }

    [GaldrJsonPropertyName("device_private_key")]
    public string DevicePrivateKey { get; set; }

    [GaldrJsonPropertyName("store_authentication_cookie")]
    public StoreAuthenticationCookie StoreAuthenticationCookie { get; set; }

    [GaldrJsonPropertyName("device_info")]
    public DeviceInfo DeviceInfo { get; set; }

    [GaldrJsonPropertyName("customer_info")]
    public CustomerInfo CustomerInfo { get; set; }

    [GaldrJsonPropertyName("expires")]
    public double Expires { get; set; }

    [GaldrJsonPropertyName("locale_code")]
    public string LocaleCode { get; set; }

    [GaldrJsonPropertyName("with_username")]
    public bool WithUsername { get; set; }

    [GaldrJsonPropertyName("activation_bytes")]
    public string ActivationBytes { get; set; }
}
