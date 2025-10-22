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
    public List<EntityEffect> Added = new();

    /// <summary>
    /// The effects ran on the target when this mutation is removed.
    /// </summary>
    [DataField]
    public List<EntityEffect> Removed = new();
}

/// <summary>
/// Data passed to entity effects that use <see cref="EffectsMutationSystem"/>.
/// </summary>
public record class MutationEntityEffectArgs : EntityEffectBaseArgs
{
    public EntityUid Mutation = EntityUid.Invalid;

    public MutationEntityEffectArgs(IEntityManager entMan) : base(EntityUid.Invalid, entMan)
    {
    }
}
