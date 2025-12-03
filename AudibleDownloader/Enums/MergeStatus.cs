namespace AudibleDownloader.Enums;

internal enum MergeStatus
{
    NotStarted = 0,
    DownloadingFFmpeg = 1,
    Trimming = 2,
    Merging = 3,
    Completed = 4,
    Failed = 5,
    Canceled = 6
}
