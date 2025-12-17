using GaldrJson;

namespace AudibleDownloader.Models.Audible;

[GaldrJsonSerializable]
internal class LibraryResponse
{
    [GaldrJsonPropertyName("items")]
    public LibraryItem[] Items { get; set; }
}
