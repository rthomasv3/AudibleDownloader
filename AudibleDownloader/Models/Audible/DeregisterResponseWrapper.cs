using GaldrJson;

namespace AudibleDownloader.Models.Audible;

internal class DeregisterResponseWrapper
{
    [GaldrJsonPropertyName("success")]
    public EmptyObject Success { get; set; }
}
