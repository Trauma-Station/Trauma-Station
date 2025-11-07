using Content.Shared.Damage;
using Robust.Shared.GameStates;

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
