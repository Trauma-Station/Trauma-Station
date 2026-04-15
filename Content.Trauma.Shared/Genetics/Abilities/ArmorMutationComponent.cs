// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;

namespace Content.Trauma.Shared.Genetics.Abilities;

/// <summary>
/// Modifies incoming damage for the mutated mob.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ArmorMutationSystem))]
public sealed partial class ArmorMutationComponent : Component
{
    [DataField(required: true)]
    public DamageModifierSet Modifiers = new();
}
