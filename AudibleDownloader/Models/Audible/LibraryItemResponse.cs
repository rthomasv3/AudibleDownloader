using GaldrJson;

namespace AudibleDownloader.Models.Audible;

[GaldrJsonSerializable]
internal class LibraryItemResponse
{
    [GaldrJsonPropertyName("item")]
    public LibraryItem Item { get; set; }
}
