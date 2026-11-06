// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Lavaland.Shared.Megafauna.Events;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Megafauna;

public sealed partial class MegafaunaStartupEffectsSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnPhaseChange(Entity<MegafaunaStartupEffectsComponent> ent, ref MegafaunaStartupEvent args)
    {
        _effects.ApplyEffects(ent, ent.Comp.Effects);
    }
}
