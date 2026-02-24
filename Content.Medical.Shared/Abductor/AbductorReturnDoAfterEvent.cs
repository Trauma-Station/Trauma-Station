// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Medical.Shared.Abductor;

[Serializable, NetSerializable]
public sealed partial class AbductorReturnDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class AbductorGizmoMarkDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class AbductorSendYourselfDoAfterEvent : DoAfterEvent
{
    [DataField("coordinates", required: true)]
    public NetCoordinates TargetCoordinates;

    private AbductorSendYourselfDoAfterEvent()
    {
    }

    public AbductorSendYourselfDoAfterEvent(NetCoordinates coords) => TargetCoordinates = coords;

    public override DoAfterEvent Clone() => new AbductorSendYourselfDoAfterEvent(TargetCoordinates);
}

[Serializable, NetSerializable]
public sealed partial class AbductorAttractDoAfterEvent : DoAfterEvent
{
    [DataField("coordinates", required: true)]
    public NetCoordinates TargetCoordinates;

    [DataField(required: true)]
    public NetEntity Victim;

    private AbductorAttractDoAfterEvent()
    {
    }

    public AbductorAttractDoAfterEvent(NetCoordinates coords, NetEntity target)
    {
        TargetCoordinates = coords;
        Victim = target;
    }

    public override DoAfterEvent Clone() => new AbductorAttractDoAfterEvent(TargetCoordinates, Victim);
}
