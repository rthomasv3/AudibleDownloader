using GaldrJson;

namespace AudibleDownloader.Models.Audible;

internal class DeviceInfo
{
    [GaldrJsonPropertyName("device_name")]
    public string DeviceName { get; set; }

    [GaldrJsonPropertyName("device_serial_number")]
    public string DeviceSerialNumber { get; set; }

    [GaldrJsonPropertyName("device_type")]
    public string DeviceType { get; set; }
}
