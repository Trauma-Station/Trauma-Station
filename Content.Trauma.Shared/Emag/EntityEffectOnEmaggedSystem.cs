// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Emag.Systems;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Emag;

public sealed partial class EntityEffectOnEmaggedSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnEmagged(Entity<EntityEffectOnEmaggedComponent> ent, ref GotEmaggedEvent args)
    {
        _effects.ApplyEffects(ent.Owner, ent.Comp.Effects, ent.Comp.Scale, args.UserUid);
    }
}
