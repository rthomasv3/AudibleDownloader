using GaldrJson;

namespace AudibleDownloader.Models.Audible;

internal class DeregisterResponseWrapper
{
    [GaldrJsonPropertyName("success")]
    public DeregisterSuccess Success { get; set; }
}

internal class DeregisterSuccess
{
    
}