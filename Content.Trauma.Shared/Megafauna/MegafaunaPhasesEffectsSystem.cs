// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Lavaland.Shared.MobPhases;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Megafauna;

public sealed partial class MegafaunaPhasesEffectsSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnPhaseChange(Entity<MegafaunaPhasesEffectsComponent> ent, ref MobPhaseChangedEvent args)
    {
        if (!ent.Comp.PhaseEffects.TryGetValue(args.Phase, out var effects))
            return;

        _effects.ApplyEffects(ent, effects);
    }
}
