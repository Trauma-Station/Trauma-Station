namespace Content.Trauma.Shared.Spy.Ui;

[Serializable, NetSerializable]
public sealed class SpyUpdateState(TimeSpan nextRefresh, HashSet<SpyBounty> bounties) : BoundUserInterfaceState
{
    public readonly TimeSpan NextRefresh = nextRefresh;

    public HashSet<SpyBounty> Bounties = bounties;
}

[Serializable, NetSerializable]
public enum SpyUplinkUiKey : byte
{
    Key,
}
