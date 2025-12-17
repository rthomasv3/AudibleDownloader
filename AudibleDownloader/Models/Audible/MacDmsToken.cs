using GaldrJson;

namespace AudibleDownloader.Models.Audible;

internal class MacDmsToken
{
    [GaldrJsonPropertyName("adp_token")]
    public string AdpToken { get; set; }

    [GaldrJsonPropertyName("device_private_key")]
    public string DevicePrivateKey { get; set; }
}
