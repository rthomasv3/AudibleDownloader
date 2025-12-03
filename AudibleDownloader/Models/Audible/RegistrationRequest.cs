using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;

internal class RegistrationRequest
{
    [JsonPropertyName("requested_token_type")]
    public string[] RequestedTokenType { get; set; }

    [JsonPropertyName("cookies")]
    public CookiesRequest Cookies { get; set; }

    [JsonPropertyName("registration_data")]
    public RegistrationData RegistrationData { get; set; }

    [JsonPropertyName("auth_data")]
    public AuthData AuthData { get; set; }

    [JsonPropertyName("requested_extensions")]
    public string[] RequestedExtensions { get; set; }
}
