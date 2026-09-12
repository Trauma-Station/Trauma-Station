// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Trauma.Shared.Guardian.Components;

/// <summary>
/// Configures the Assassin holoparasite variant. It can enter a brief stealth burst that
/// amplifies its next attack, and its blades are coated in poison.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GuardianAssassinComponent : Component
{
    // Stealth burst
    [DataField]
    public EntProtoId StealthEffect = "ForcedStealthStatusEffectAssassin";

    [DataField]
    public TimeSpan StealthDuration = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The damage multiplier applied to the assassin's melee attacks while stealthed.
    /// </summary>
    [DataField]
    public float StealthDamageMultiplier = 1.1f;

    /// <summary>
    /// Damage dealt by the next attack out of stealth, replacing the regular melee damage.
    /// </summary>
    [DataField]
    public DamageSpecifier StealthAttackDamage = new()
    {
        DamageDict = new()
        {
            ["Blunt"] = FixedPoint2.New(50),
            ["Poison"] = FixedPoint2.New(15)
        }
    };
}
