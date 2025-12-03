namespace AudibleDownloader.Models;

internal class DownloadBookResult
{
    public bool Success { get; set; }
    public string Directory { get; set; }
    public bool IsMerged { get; set; }
}
