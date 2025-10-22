using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.Genetics.Abilities;

/// <summary>
/// Handles running effects for <see cref="EffectsMutationComponent"/>.
/// Also provides API for effects on trigger mutation.
/// </summary>
public sealed class EffectsMutationSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private MutationEntityEffectArgs _args;

    public override void Initialize()
    {
        base.Initialize();

        _args = new MutationEntityEffectArgs(EntityManager);

        SubscribeLocalEvent<EffectsMutationComponent, MutationAddedEvent>(OnAdded);
        SubscribeLocalEvent<EffectsMutationComponent, MutationRemovedEvent>(OnRemoved);
    }

    private void OnAdded(Entity<EffectsMutationComponent> ent, ref MutationAddedEvent args)
    {
        RunEffects(ent, args.Target, ent.Comp.Added);
    }

    private void OnRemoved(Entity<EffectsMutationComponent> ent, ref MutationRemovedEvent args)
    {
        RunEffects(ent, args.Target, ent.Comp.Removed);
    }

    /// <summary>
    /// Runs a list of effects for a mutation on a target mob.
    /// </summary>
    public void RunEffects(EntityUid mutation, EntityUid target, List<EntityEffect> effects)
    {
        _args.TargetEntity = target;
        _args.Mutation = mutation;
        foreach (var effect in effects)
        {
            if (effect.ShouldApply(_args, _random))
                effect.Effect(_args);
        }
    }
}
