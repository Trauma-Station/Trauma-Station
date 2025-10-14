using Content.Shared.Actions.Events;
using Content.Shared.EntityEffects;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.Actions;

public sealed class EffectActionSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EffectActionComponent, ActionPerformedEvent>(OnActionPerformed);
    }

    private void OnActionPerformed(Entity<EffectActionComponent> ent, ref ActionPerformedEvent args)
    {
        var target = args.Performer;
        var effectArgs = new EntityEffectBaseArgs(target, EntityManager);
        foreach (var effect in ent.Comp.Effects)
        {
            if (effect.ShouldApply(effectArgs, _random))
                effect.Effect(effectArgs);
        }
    }
}
