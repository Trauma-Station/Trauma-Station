// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;

namespace Content.Trauma.Shared.BloodCult.Runes.Revive;

[RegisterComponent, NetworkedComponent]
public sealed partial class CultRuneReviveComponent : Component
{
    [DataField]
    public float ReviveRange = 0.5f;

    // TODO: why not just rejuv, it's magic anyway
    [DataField]
    public DamageSpecifier Healing = new()
    {
        DamageDict = new()
        {
            ["Blunt"] = -100,
            ["Slash"] = -100,
            ["Piercing"] = -100,
            ["Ballistic"] = -100,
            ["Heat"] = -100,
            ["Cold"] = -100,
            ["Shock"] = -100,
            ["Caustic"] = -100,
            ["Asphyxiation"] = -100,
            ["Bloodloss"] = -100,
            ["Poison"] = -50,
            ["Radiation"] = -50,
            ["Cellular"] = -50
        }
    };
}
