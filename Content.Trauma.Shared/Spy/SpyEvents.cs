using Content.Shared.DoAfter;

namespace Content.Trauma.Shared.Spy;

[Serializable, NetSerializable]
public sealed partial class SpyStealDoAfterEvent : DoAfterEvent
{
    [DataField]
    public ProtoId<SpyBountyPrototype> Bounty;

    [DataField]
    public NetEntity Rule;

    [DataField]
    public NetEntity StealTarget;

    public SpyStealDoAfterEvent() { }

    public SpyStealDoAfterEvent(ProtoId<SpyBountyPrototype> bounty, NetEntity rule, NetEntity stealTarget)
    {
        Bounty = bounty;
        Rule = rule;
        StealTarget = stealTarget;
    }

    public override DoAfterEvent Clone() => this;
}
