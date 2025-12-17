using GaldrJson;

namespace AudibleDownloader.Models.Audible;

[GaldrJsonSerializable]
internal class DeregisterRequest
{
    [GaldrJsonPropertyName("deregister_all_existing_accounts")]
    public bool DeregisterAllExistingAccounts { get; set; }
}
