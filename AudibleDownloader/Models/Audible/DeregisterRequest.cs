using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;

internal class DeregisterRequest
{
    [JsonPropertyName("deregister_all_existing_accounts")]
    public bool DeregisterAllExistingAccounts { get; set; }
}
