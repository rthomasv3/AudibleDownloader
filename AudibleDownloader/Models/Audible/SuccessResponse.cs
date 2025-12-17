using GaldrJson;

namespace AudibleDownloader.Models.Audible;

internal class SuccessResponse
{
    [GaldrJsonPropertyName("tokens")]
    public Tokens Tokens { get; set; }

    [GaldrJsonPropertyName("extensions")]
    public Extensions Extensions { get; set; }
}
