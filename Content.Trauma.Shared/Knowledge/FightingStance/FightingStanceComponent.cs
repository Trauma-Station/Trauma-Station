// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Knowledge.FightingStance;

[RegisterComponent, NetworkedComponent]
public sealed partial class FightingStanceComponent : Component
{
    [DataField]
    public int AttackMod;

    [DataField]
    public int DefenseMod;

    [DataField]
    public int SpeedMod;

    [DataField]
    public int DamageMod;

    [DataField]
    public int DefenseDice;
}
