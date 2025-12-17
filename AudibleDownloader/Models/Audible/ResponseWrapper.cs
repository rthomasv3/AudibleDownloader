using GaldrJson;

namespace AudibleDownloader.Models.Audible;

internal class ResponseWrapper
{
    [GaldrJsonPropertyName("success")]
    public SuccessResponse Success { get; set; }
}
