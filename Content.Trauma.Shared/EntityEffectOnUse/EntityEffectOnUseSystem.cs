using Content.Shared.Interaction.Events;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffectOnUse;

public sealed partial class EntityEffectOnUseSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [ SubscribeLocalEvent ]
    private void EffectsOnUse(Entity<EntityEffectOnUseComponent> ent, ref UseInHandEvent args)
    {
        if (ent.Comp.ApplyToUser)
        {
            _effects.ApplyEffects(args.User, ent.Comp.Effects);
            return;
        }
        _effects.ApplyEffects(ent, ent.Comp.Effects);
    }
}