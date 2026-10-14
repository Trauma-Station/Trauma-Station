// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Factory.Shared.Machines;
using Content.Server.Construction.Components;
using Content.Shared.Timing.Components;
using Content.Shared.Timing.Systems;

namespace Content.Factory.Server.Machines;

public sealed partial class ServerInteractorSystem : InteractorSystem
{
    [Dependency] private UseDelaySystem _useDelay = default!;
    [Dependency] private EntityQuery<ConstructionComponent> _constructionQuery = default!;
    [Dependency] private EntityQuery<UseDelayComponent> _useDelayQuery = default!;

    [SubscribeLocalEvent]
    private void OnStarted(Entity<InteractorComponent> ent, ref MachineStartedEvent args)
    {
        // don't let it get spammed every tick to avoid lag machines
        if (_useDelayQuery.TryComp(ent, out var delay) && !_useDelay.TryResetDelay((ent, delay), true))
            return;

        // another doafter is already running
        if (HasDoAfter(ent))
        {
            Machine.Failed(ent.Owner);
            return;
        }

        // skip finding a target for use in hand, it's unused.
        var target = ent.Comp.UseInHand ? null : FindTarget(ent);

        _constructionQuery.TryComp(target, out var construction);
        var originalCount = construction?.InteractionQueue.Count ?? 0;
        if (!TryInteractWith(ent, target))
        {
            // have to remove it since user's filter was bad due to unhandled interaction
            Machine.Failed(ent.Owner);
            return;
        }

        // construction supercode queues it instead of starting a doafter now, assume that queuing means it has started
        var newCount = construction?.InteractionQueue?.Count ?? 0;
        var doing = HasDoAfter(ent);
        Machine.Started(ent.Owner);
        if (newCount > originalCount || doing)
        {
            UpdateAppearance(ent, InteractorState.Active);
            if (doing) // for steps with doafter they just get queued
                Machine.Completed(ent.Owner);
        }
        else
        {
            // no doafter, complete it immediately
            Machine.Completed(ent.Owner);
            UpdateAppearance(ent);
        }
    }
}
