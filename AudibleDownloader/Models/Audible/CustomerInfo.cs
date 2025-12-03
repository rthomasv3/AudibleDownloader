using System.Text.Json.Serialization;

namespace AudibleDownloader.Models.Audible;

internal class CustomerInfo
{
    [JsonPropertyName("account_pool")]
    public string AccountPool { get; set; }

    [JsonPropertyName("user_id")]
    public string UserId { get; set; }

    [JsonPropertyName("home_region")]
    public string HomeRegion { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("given_name")]
    public string GivenName { get; set; }
}
