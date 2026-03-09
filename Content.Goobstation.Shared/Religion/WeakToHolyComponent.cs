// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Religion;

[RegisterComponent, NetworkedComponent]
public sealed partial class WeakToHolyComponent : Component
{
    /// <summary>
    /// Is the entity currently standing on a rune?
    /// </summary>
    [ViewVariables]
    public bool IsColliding;

    /// <summary>
    /// Duration between each heal tick.
    /// </summary>
    [DataField]
    public TimeSpan HealTickDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Used for passive healing.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextPassiveHealTick;


    /// <summary>
    /// How much the entity is healed by runes each tick.
    /// </summary>
    [DataField]
    public DamageSpecifier HealAmount = new()
    {
        DamageDict =
        {
            ["Holy"] = -4,
        },
    };

    /// <summary>
    /// How much the entity is healed passively by each tick.
    /// </summary>
    [DataField]
    public DamageSpecifier PassiveAmount = new()
    {
        DamageDict =
        {
            ["Holy"] = -0.5, // if its less it dont work du to limb damage
        },
    };
}
