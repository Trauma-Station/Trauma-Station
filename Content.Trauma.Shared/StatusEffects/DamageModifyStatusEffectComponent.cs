// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;

namespace Content.Trauma.Shared.StatusEffects;

/// <summary>
/// Status effect that modifies damage taken by a multiplier
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DamageModifyStatusEffectComponent : Component
{
    /// <summary>
    /// A modifier set to adjust <see cref="DamageModifyEvent"/>.
    /// </summary>
    [DataField(required: true)]
    public DamageModifierSet Modifiers = new();
}
