// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Interaction.Events;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffectOnUse;

public sealed partial class EntityEffectOnUseSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void EffectsOnUse(Entity<EntityEffectOnUseComponent> ent, ref UseInHandEvent args)
    {
        if (ent.Comp.EffectsToUser != null)
            _effects.ApplyEffects(args.User, ent.Comp.EffectsToUser);

        if (ent.Comp.EffectsToUsed != null)
            _effects.ApplyEffects(args.User, ent.Comp.EffectsToUsed);
    }
}
