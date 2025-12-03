using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;

internal class MacDmsToken
{
    [JsonPropertyName("adp_token")]
    public string AdpToken { get; set; }

    [JsonPropertyName("device_private_key")]
    public string DevicePrivateKey { get; set; }
}
