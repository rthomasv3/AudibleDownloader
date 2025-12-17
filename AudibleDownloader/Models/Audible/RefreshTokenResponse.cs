using GaldrJson;

namespace AudibleDownloader.Models.Audible;

[GaldrJsonSerializable]
internal class RefreshTokenResponse
{
    [GaldrJsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [GaldrJsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}
