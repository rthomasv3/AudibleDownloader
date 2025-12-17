using GaldrJson;

namespace AudibleDownloader.Models.Audible;

internal class AvailableCodec
{
    [GaldrJsonPropertyName("enhanced_codec")]
    public string EnhancedCodec { get; set; }

    [GaldrJsonPropertyName("name")]
    public string Name { get; set; }

    [GaldrJsonPropertyName("format")]
    public string Format { get; set; }
}
