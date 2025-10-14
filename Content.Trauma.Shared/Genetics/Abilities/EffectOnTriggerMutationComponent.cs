using Content.Shared.EntityEffects;
using Content.Shared.Mobs;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Genetics.Abilities;

/// <summary>
/// Runs entity effects on the mutation target when this mutation is triggered.
/// The mob state can also be filtered.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EffectOnTriggerMutationComponent : Component
{
    /// <summary>
    /// What mob states the target is allowed to be in.
    /// </summary>
    [DataField(required: true)]
    public HashSet<MobState> States = new();

    /// <summary>
    /// The effects to run on the target.
    /// </summary>
    [DataField(required: true)]
    public List<EntityEffect> Effects = new();
}

/// <summary>
/// Data passed to entity effects of <see cref="EffectOnTriggerMutationComponent.Effects"/>.
/// </summary>
public record class MutationEntityEffectArgs : EntityEffectBaseArgs
{
    public EntityUid Mutation = EntityUid.Invalid;

    public MutationEntityEffectArgs(IEntityManager entMan) : base(EntityUid.Invalid, entMan)
    {
    }
}
