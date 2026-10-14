// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.StatusEffects;

public sealed partial class StatusEffectEffectsApplySystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnApplied(Entity<StatusEffectEffectsApplyComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (ent.Comp.EffectsOnApply is not { } effectsOnApply ||
            _timing.ApplyingState) // it's assumed that the effects will be networked or adding the status effect is predicted
            return;

        _effects.ApplyEffects(args.Target, effectsOnApply);
    }

    [SubscribeLocalEvent]
    private void OnRemoval(Entity<StatusEffectEffectsApplyComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (ent.Comp.EffectsOnRemoval is not { } effectsOnRemoval ||
            _timing.ApplyingState ||
            TerminatingOrDeleted(args.Target))
            return;

        _effects.ApplyEffects(args.Target, effectsOnRemoval);
    }
}
