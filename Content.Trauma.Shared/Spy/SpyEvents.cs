using Content.Shared.DoAfter;

namespace Content.Trauma.Shared.Spy;

[Serializable, NetSerializable]
public sealed partial class SpyStealDoAfterEvent(SpyBounty bounty, NetEntity rule) : DoAfterEvent
{
    [DataField]
    public SpyBounty Bounty = bounty;

    [DataField]
    public NetEntity Rule = rule;

    public SpyStealDoAfterEvent() : this(new SpyBounty(), NetEntity.Invalid) { }

    public override DoAfterEvent Clone() => new SpyStealDoAfterEvent(Bounty, Rule);
}
