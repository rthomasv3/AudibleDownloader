using GaldrJson;

namespace AudibleDownloader.Models.Audible;

[GaldrJsonSerializable]
internal class DeregisterResponse
{
    [GaldrJsonPropertyName("response")]
    public DeregisterResponseWrapper Response { get; set; }
}
