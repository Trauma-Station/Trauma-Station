using Content.Shared.EntityEffects;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Trauma.Shared.Genetics.Abilities;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.Random;

namespace Content.Trauma.Server.Genetics.Abilities;

public sealed class EffectOnTriggerMutationSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;

    private EntityQuery<MobStateComponent> _mobQuery;
    private MutationEntityEffectArgs _args;

    public override void Initialize()
    {
        base.Initialize();

        _mobQuery = GetEntityQuery<MobStateComponent>();
        _args = new MutationEntityEffectArgs(EntityManager);

        SubscribeLocalEvent<EffectOnTriggerMutationComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<EffectOnTriggerMutationComponent> ent, ref TriggerEvent args)
    {
        if (_mutation.GetMutationTarget(ent) is not {} target ||
            !_mobQuery.TryComp(target, out var mob) ||
            !ent.Comp.States.Contains(mob.CurrentState))
        {
            return;
        }

        _args.TargetEntity = target;
        _args.Mutation = ent;
        foreach (var effect in ent.Comp.Effects)
        {
            if (effect.ShouldApply(_args, _random))
                effect.Effect(_args);
        }
    }
}
