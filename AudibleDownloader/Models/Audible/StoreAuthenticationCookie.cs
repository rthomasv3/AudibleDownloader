using GaldrJson;

namespace AudibleDownloader.Models.Audible;

internal class StoreAuthenticationCookie
{
    [GaldrJsonPropertyName("cookie")]
    public string Cookie { get; set; }
}
