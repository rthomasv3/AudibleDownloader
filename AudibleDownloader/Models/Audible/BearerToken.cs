using GaldrJson;

namespace AudibleDownloader.Models.Audible;

internal class BearerToken
{
    [GaldrJsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [GaldrJsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }

    [GaldrJsonPropertyName("expires_in")]
    public string ExpiresIn { get; set; }
}
