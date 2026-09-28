// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;

namespace Content.Trauma.Shared.BloodCult.Runes.Offering;

[RegisterComponent, NetworkedComponent]
public sealed partial class CultRuneOfferingComponent : Component
{
    /// <summary>
    ///     The lookup range for offering targets
    /// </summary>
    [DataField]
    public float OfferingRange = 0.5f;

    /// <summary>
    ///     The amount of cultists require to convert a living target.
    /// </summary>
    [DataField]
    public int ConvertInvokersAmount = 2;

    /// <summary>
    ///     The amount of cultists required to sacrifice a living target.
    /// </summary>
    [DataField]
    public int AliveSacrificeInvokersAmount = 3;

    /// <summary>
    ///     The amount of charges revive rune system should recieve on sacrifice/convert.
    /// </summary>
    [DataField]
    public int ReviveChargesPerOffering = 1;

    /// <summary>
    /// Damage done to the target after converting them.
    /// Values must be negative to heal.
    /// </summary>
    [DataField]
    public DamageSpecifier ConvertHealing = new()
    {
        DamageDict = new()
        {
            ["Blunt"] = -40,
            ["Slash"] = -40,
            ["Piercing"] = -40,
            ["Ballistic"] = -40,
            ["Heat"] = -40,
            ["Cold"] = -40,
            ["Shock"] = -40,
            ["Caustic"] = -40
        }
    };
}
