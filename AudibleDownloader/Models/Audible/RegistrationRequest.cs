using GaldrJson;

namespace AudibleDownloader.Models.Audible;

[GaldrJsonSerializable]
internal class RegistrationRequest
{
    [GaldrJsonPropertyName("requested_token_type")]
    public string[] RequestedTokenType { get; set; }

    [GaldrJsonPropertyName("cookies")]
    public CookiesRequest Cookies { get; set; }

    [GaldrJsonPropertyName("registration_data")]
    public RegistrationData RegistrationData { get; set; }

    [GaldrJsonPropertyName("auth_data")]
    public AuthData AuthData { get; set; }

    [GaldrJsonPropertyName("requested_extensions")]
    public string[] RequestedExtensions { get; set; }
}
