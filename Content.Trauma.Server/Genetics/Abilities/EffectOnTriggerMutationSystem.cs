using Content.Server.Explosion.EntitySystems;
using Content.Trauma.Shared.Genetics.Abilities;
using Content.Trauma.Shared.Genetics.Mutations;

namespace Content.Trauma.Server.Genetics.Abilities;

public sealed class EffectOnTriggerMutationSystem : EntitySystem
{
    [Dependency] private readonly EffectsMutationSystem _effects = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EffectOnTriggerMutationComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<EffectOnTriggerMutationComponent> ent, ref TriggerEvent args)
    {
        if (_mutation.GetMutationTarget(ent) is not {} target)
            return;

        _effects.RunEffects(ent, target, ent.Comp.Effects);
    }
}
