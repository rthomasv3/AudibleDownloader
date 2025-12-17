using GaldrJson;

namespace AudibleDownloader.Models.Audible;

internal class RegistrationData
{
    [GaldrJsonPropertyName("domain")]
    public string Domain { get; set; }

    [GaldrJsonPropertyName("app_version")]
    public string AppVersion { get; set; }

    [GaldrJsonPropertyName("device_serial")]
    public string DeviceSerial { get; set; }

    [GaldrJsonPropertyName("device_type")]
    public string DeviceType { get; set; }

    [GaldrJsonPropertyName("device_name")]
    public string DeviceName { get; set; }

    [GaldrJsonPropertyName("os_version")]
    public string OsVersion { get; set; }

    [GaldrJsonPropertyName("software_version")]
    public string SoftwareVersion { get; set; }

    [GaldrJsonPropertyName("device_model")]
    public string DeviceModel { get; set; }

    [GaldrJsonPropertyName("app_name")]
    public string AppName { get; set; }
}
