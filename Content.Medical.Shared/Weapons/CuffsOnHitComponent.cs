// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;

namespace Content.Medical.Shared.Weapons;

[RegisterComponent, NetworkedComponent]
public sealed partial class CuffsOnHitComponent : Component
{
    [DataField("proto", required: true)]
    public EntProtoId HandcuffPrototype;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(1);
}

[Serializable, NetSerializable]
public sealed partial class CuffsOnHitDoAfterEvent : SimpleDoAfterEvent;
