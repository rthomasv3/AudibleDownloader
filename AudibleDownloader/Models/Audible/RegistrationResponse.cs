using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;

internal class RegistrationResponse
{
    [JsonPropertyName("response")]
    public ResponseWrapper Response { get; set; }
}
