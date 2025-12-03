namespace AudibleDownloader;

internal class Config
{
    public string AuthFilePath => "auth.json";
    public bool IsLoggedIn { get; set; }
    public string LibraryPath { get; set; }
    public string SettingsPath { get; set; }
}
