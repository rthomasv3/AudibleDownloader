using GaldrJson;

namespace AudibleDownloader.Models.Audible;

[GaldrJsonSerializable]
internal class RegistrationResponse
{
    [GaldrJsonPropertyName("response")]
    public ResponseWrapper Response { get; set; }
}
