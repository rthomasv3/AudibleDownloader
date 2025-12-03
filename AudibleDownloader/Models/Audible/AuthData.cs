using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;

internal class AuthData
{
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; }

    [JsonPropertyName("authorization_code")]
    public string AuthorizationCode { get; set; }

    [JsonPropertyName("code_verifier")]
    public string CodeVerifier { get; set; }

    [JsonPropertyName("code_algorithm")]
    public string CodeAlgorithm { get; set; }

    [JsonPropertyName("client_domain")]
    public string ClientDomain { get; set; }
}
