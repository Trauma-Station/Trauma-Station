using Content.Shared.Store;

namespace Content.Trauma.Shared.Spy.Ui;

[Serializable, NetSerializable]
public sealed class SpyUpdateState(
    TimeSpan nextRefresh,
    HashSet<SpyBounty> bounties,
    Dictionary<NetEntity, List<string>> rewards) : BoundUserInterfaceState
{
    public TimeSpan NextRefresh = nextRefresh;

    public HashSet<SpyBounty> Bounties = bounties;

    public Dictionary<NetEntity, List<string>> Rewards = rewards;
}

[Serializable, NetSerializable]
public sealed class SpyRewardSelectedMessage(string id, ProtoId<ListingPrototype> listing) : BoundUserInterfaceMessage
{
    public string Id = id;

    public ProtoId<ListingPrototype> Listing = listing;
}

[Serializable, NetSerializable]
public enum SpyUplinkUiKey : byte
{
    Key,
}
