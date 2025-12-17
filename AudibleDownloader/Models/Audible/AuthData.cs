using GaldrJson;

namespace AudibleDownloader.Models.Audible;

internal class AuthData
{
    [GaldrJsonPropertyName("client_id")]
    public string ClientId { get; set; }

    [GaldrJsonPropertyName("authorization_code")]
    public string AuthorizationCode { get; set; }

    [GaldrJsonPropertyName("code_verifier")]
    public string CodeVerifier { get; set; }

    [GaldrJsonPropertyName("code_algorithm")]
    public string CodeAlgorithm { get; set; }

    [GaldrJsonPropertyName("client_domain")]
    public string ClientDomain { get; set; }
}
