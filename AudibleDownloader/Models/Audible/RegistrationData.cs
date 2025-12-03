using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;

internal class RegistrationData
{
    [JsonPropertyName("domain")]
    public string Domain { get; set; }

    [JsonPropertyName("app_version")]
    public string AppVersion { get; set; }

    [JsonPropertyName("device_serial")]
    public string DeviceSerial { get; set; }

    [JsonPropertyName("device_type")]
    public string DeviceType { get; set; }

    [JsonPropertyName("device_name")]
    public string DeviceName { get; set; }

    [JsonPropertyName("os_version")]
    public string OsVersion { get; set; }

    [JsonPropertyName("software_version")]
    public string SoftwareVersion { get; set; }

    [JsonPropertyName("device_model")]
    public string DeviceModel { get; set; }

    [JsonPropertyName("app_name")]
    public string AppName { get; set; }
}
