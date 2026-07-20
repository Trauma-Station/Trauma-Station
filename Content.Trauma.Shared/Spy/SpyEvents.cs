using Content.Shared.DoAfter;

namespace Content.Trauma.Shared.Spy;

[Serializable, NetSerializable]
public sealed partial class SpyStealDoAfterEvent : DoAfterEvent
{
    [DataField]
    public ProtoId<SpyBountyPrototype> Bounty;

    [DataField]
    public NetEntity Rule;

    public SpyStealDoAfterEvent() { }

    public SpyStealDoAfterEvent(ProtoId<SpyBountyPrototype> bounty, NetEntity rule)
    {
        Bounty = bounty;
        Rule = rule;
    }

    public override DoAfterEvent Clone() => this;
}
