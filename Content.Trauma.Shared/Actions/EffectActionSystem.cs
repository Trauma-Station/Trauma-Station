using Content.Shared.Actions.Events;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Actions;

public sealed class EffectActionSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EffectActionComponent, ActionPerformedEvent>(OnActionPerformed);
        SubscribeLocalEvent<EffectActionComponent, EffectActionEvent>(OnEffectAction);
    }

    private void OnActionPerformed(Entity<EffectActionComponent> ent, ref ActionPerformedEvent args)
    {
        if (!ent.Comp.TargetUser)
            return;

        var target = args.Performer;
        _effects.ApplyEffects(target, ent.Comp.Effects);
    }

    private void OnEffectAction(Entity<EffectActionComponent> ent, ref EffectActionEvent args)
    {
        var target = args.Target;
        _effects.ApplyEffects(target, ent.Comp.Effects);
    }
}
