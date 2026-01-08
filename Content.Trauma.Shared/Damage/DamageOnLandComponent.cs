// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Damage;

/// <summary>
/// Trauma - moved out of server and cleaned it up.
/// Deals damage to this entity when it lands after being thrown.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DamageOnLandComponent : Component
{
    /// <summary>
    /// Should this entity be damaged when it lands regardless of its resistances?
    /// </summary>
    [DataField]
    public bool IgnoreResistances;

    /// <summary>
    /// How much damage to deal.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage = default!;
}
