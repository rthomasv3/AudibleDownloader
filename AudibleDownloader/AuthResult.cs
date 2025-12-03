namespace AudibleDownloader;

internal class AuthResult
{
    public string AuthorizationCode { get; set; }
    public string CodeVerifier { get; set; }
    public string Domain { get; set; }
    public string Serial { get; set; }
}
