namespace AudibleDownloader.Enums;

internal enum DownloadStatus
{
    NotStarted = 0,
    Downloading = 1,
    Decrypting = 2,
    Completed = 3,
    Failed = 4,
    Canceled = 5
}
