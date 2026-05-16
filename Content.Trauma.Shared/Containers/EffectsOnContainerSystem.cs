// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Containers;

public sealed partial class EffectsOnContainerSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EffectsOnContainerComponent, EntInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<EffectsOnContainerComponent, EntRemovedFromContainerMessage>(OnRemovedFromContainer);
    }

    private void OnInsertedIntoContainer(Entity<EffectsOnContainerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        if (ent.Comp.ContainerId != null && ent.Comp.ContainerId != args.Container.ID)
            return;

        _effects.ApplyEffects(args.Entity, ent.Comp.Inserted, user: ent.Owner);
    }

    private void OnRemovedFromContainer(Entity<EffectsOnContainerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        if (ent.Comp.ContainerId != null && ent.Comp.ContainerId != args.Container.ID)
            return;

        _effects.ApplyEffects(args.Entity, ent.Comp.Removed, user: ent.Owner);
    }
}
