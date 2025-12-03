using System.Collections.Generic;
using AudibleDownloader.Models.Audible;

namespace AudibleDownloader.Models;

internal class GetLibraryResult
{
    public List<LibraryResult> Items { get; set; }
}
