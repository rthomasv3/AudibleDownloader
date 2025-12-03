using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;

internal class ResponseWrapper
{
    [JsonPropertyName("success")]
    public SuccessResponse Success { get; set; }
}
