using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;

internal class DeviceInfo
{
    [JsonPropertyName("device_name")]
    public string DeviceName { get; set; }

    [JsonPropertyName("device_serial_number")]
    public string DeviceSerialNumber { get; set; }

    [JsonPropertyName("device_type")]
    public string DeviceType { get; set; }
}
