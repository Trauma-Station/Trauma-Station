// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Heretic.Components.Side;

[RegisterComponent, NetworkedComponent]
public sealed partial class ShatteredRisenComponent : Component
{
    [DataField]
    public EntProtoId Weapon1 = "ArmBladeShattered";

    [DataField]
    public EntProtoId Weapon2 = "ArmHammerShattered";
}
