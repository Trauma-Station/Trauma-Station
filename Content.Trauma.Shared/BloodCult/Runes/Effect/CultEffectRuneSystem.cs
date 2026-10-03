// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.BloodCult.Runes.Barrier;

public sealed partial class CultEffectRuneSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnRuneInvoke(Entity<CultEffectRuneComponent> ent, ref RuneInvokeEvent args)
    {
        if (args.Handled)
            return;

        _effects.ApplyEffects(ent, ent.Comp.Effects, user: args.User);
        args.Handled = true;
    }
}
