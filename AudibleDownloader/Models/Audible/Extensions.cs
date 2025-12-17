using GaldrJson;

namespace AudibleDownloader.Models.Audible;

internal class Extensions
{
    [GaldrJsonPropertyName("device_info")]
    public DeviceInfo DeviceInfo { get; set; }

    [GaldrJsonPropertyName("customer_info")]
    public CustomerInfo CustomerInfo { get; set; }
}
