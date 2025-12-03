using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;

internal class Extensions
{
    [JsonPropertyName("device_info")]
    public DeviceInfo DeviceInfo { get; set; }

    [JsonPropertyName("customer_info")]
    public CustomerInfo CustomerInfo { get; set; }
}
