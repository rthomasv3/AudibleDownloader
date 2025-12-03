namespace AudibleDownloader.Models;

internal class DecryptedBookResult
{
    public string SafeFileName { get; set; }
    public string SafeAuthorName { get; set; }
    public string SafeTitle { get; set; }
    public byte[] FileData { get; set; }
}
