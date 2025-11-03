using Content.Shared.EntityEffects;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Genetics.Abilities;

/// <summary>
/// Runs entity effects when this mutation is added or removed.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(EffectsMutationSystem))]
public sealed partial class EffectsMutationComponent : Component
{
    /// <summary>
    /// The effects ran on the target when this mutation is added.
    /// </summary>
    [DataField]
    public EntityEffect[] Added = [];

    /// <summary>
    /// The effects ran on the target when this mutation is removed.
    /// </summary>
    [DataField]
    public EntityEffect[] Removed = [];
}
