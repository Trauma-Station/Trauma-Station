// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusEffectNew;

using Content.Trauma.Shared.Genetics.Mutations;

namespace Content.Trauma.Shared.Genetics.Abilities;

public sealed partial class StatusEffectsMutationSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _status = default!;

    [SubscribeLocalEvent]
    private void OnAdded(Entity<StatusEffectsMutationComponent> ent, ref MutationAddedEvent args)
    {
        _status.AddEffects(args.Target, ent.Comp.StatusEffects);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<StatusEffectsMutationComponent> ent, ref MutationRemovedEvent args)
    {
        _status.RemoveEffects(args.Target, ent.Comp.StatusEffects);
    }
}
