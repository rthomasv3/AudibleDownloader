using AudibleDownloader.Enums;

namespace AudibleDownloader.Models;

internal class MergeProgressResult
{
    public double Progress { get; set; }
    public string Message { get; set; }
    public int Status { get; set; }
}
