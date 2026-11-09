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
        if (ent.Comp.UserEffects is { } userEffects)
            _effects.ApplyEffects(args.User, userEffects);

        if (ent.Comp.Effects is { } effects)
            _effects.ApplyEffects(ent, effects);

        args.Handled = true;
    }
}
