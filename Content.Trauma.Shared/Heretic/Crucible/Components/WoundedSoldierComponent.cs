// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;

namespace Content.Trauma.Shared.Heretic.Crucible.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class WoundedSoldierComponent : Component
{
    [DataField]
    public float LifeStealMultiplier = 0.3f;

    [DataField]
    public float StaminaHealMultiplier = 0.3f;

    [DataField]
    public float OvertimeDamageThresholdRatio = 0.1f;

    [DataField]
    public DamageSpecifier DamageOverTime = new()
    {
        DamageDict =
        {
            { "Heat", 10 },
        },
    };

    [DataField]
    public LocId ExamineLoc = "wounded-solider-effect-examine-message";
}
