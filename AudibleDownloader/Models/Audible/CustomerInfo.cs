using GaldrJson;

namespace AudibleDownloader.Models.Audible;

internal class CustomerInfo
{
    [GaldrJsonPropertyName("account_pool")]
    public string AccountPool { get; set; }

    [GaldrJsonPropertyName("user_id")]
    public string UserId { get; set; }

    [GaldrJsonPropertyName("home_region")]
    public string HomeRegion { get; set; }

    [GaldrJsonPropertyName("name")]
    public string Name { get; set; }

    [GaldrJsonPropertyName("given_name")]
    public string GivenName { get; set; }
}
