// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Content.Shared.Mind;
using Content.Shared.Stacks;
using Content.Trauma.Shared.BloodCult.Components;
using Content.Trauma.Shared.BloodCult.Spells;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.BloodCult.Spells;

public sealed partial class TwistedConstructionSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private EntityQuery<StackComponent> _stackQuery = default!;
    [Dependency] private EntityQuery<TwistedConstructionTargetComponent> _query = default!;

    [SubscribeLocalEvent]
    private void OnTwistedConstruction(BloodCultTwistedConstructionEvent args)
    {
        var target = args.Target;
        if (args.Handled || !_query.TryComp(target, out var comp))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.Performer,
            comp.DoAfterDelay,
            new TwistedConstructionDoAfterEvent(),
            eventTarget: target,
            target: target);

        args.Handled = _doAfter.TryStartDoAfter(doAfterArgs);
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<TwistedConstructionTargetComponent> target, ref TwistedConstructionDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !_timing.IsFirstTimePredicted)
            return;

        args.Handled = true;
        var pos = Transform(target).Coordinates;
        var replacement = PredictedSpawnAtPosition(target.Comp.ReplacementProto, pos);
        if (_stackQuery.TryComp(target, out var oldStack) && _stackQuery.TryComp(replacement, out var newStack))
            _stack.SetCount(replacement, oldStack.Count, newStack);

        if (_mind.TryGetMind(target, out var mindId, out var mind))
            _mind.TransferTo(mindId, replacement, mind: mind);
        // TODO: make an event if any other shit needs to be moved

        RemComp(target, target.Comp); // prevent multiple doafters on the same tick duping
        PredictedQueueDel(target);
    }
}
