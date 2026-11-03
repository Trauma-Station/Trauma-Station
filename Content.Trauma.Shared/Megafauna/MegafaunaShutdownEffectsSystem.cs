// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Lavaland.Shared.Megafauna.Events;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Megafauna;

public sealed partial class MegafaunaShutdownEffectsSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnPhaseChange(Entity<MegafaunaShutdownEffectsComponent> ent, ref MegafaunaShutdownEvent args)
    {
        _effects.ApplyEffects(ent, ent.Comp.Effects);
    }
}
