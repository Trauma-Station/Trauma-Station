// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Stacks;
using Content.Trauma.Shared.BloodCult.Components;
using Content.Trauma.Shared.BloodCult.Gamerule;
using Content.Trauma.Shared.BloodCult.Spells;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.Trauma.Shared.BloodCult.Spells;

public sealed partial class TwistedConstructionSystem : EntitySystem
{
    [Dependency] private BloodCultSystem _cult = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private EntityQuery<StackComponent> _stackQuery = default!;
    [Dependency] private EntityQuery<TwistedConstructionTargetComponent> _query = default!;

    private static readonly EntProtoId SoulShard = "SoulShard";

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
        if (args.Handled || args.Cancelled || _cult.GetRule(args.User) is not { } rule)
            return;

        args.Handled = true;
        var pos = Transform(target).Coordinates;
        var replacement = PredictedSpawnAtPosition(target.Comp.ReplacementProto, pos);

        var ev = new BloodCultTransmutedEvent(replacement, rule, pos);
        RaiseLocalEvent(target, ref ev);

        if (_mind.TryGetMind(target, out var mindId, out var mind))
            _mind.TransferTo(mindId, replacement, mind: mind);

        RemComp(target, target.Comp); // prevent multiple doafters on the same tick duping
        PredictedQueueDel(target);

        _cult.SetCultRule(replacement, rule);
    }

    [SubscribeLocalEvent]
    private void OnStackTransmuted(Entity<StackComponent> ent, ref BloodCultTransmutedEvent args)
    {
        if (_stackQuery.TryComp(args.Target, out var stack))
            _stack.SetCount((args.Target, stack), ent.Comp.Count);
    }

    [SubscribeLocalEvent]
    private void OnMindTransmuted(Entity<MindContainerComponent> ent, ref BloodCultTransmutedEvent args)
    {
        if (ent.Comp.Mind is not { } mind)
            return;

        // borg law 2 hold still
        var shard = PredictedSpawnAtPosition(SoulShard, args.Position);
        _cult.SetCultRule(shard, args.Rule);
        _mind.TransferTo(mind, shard);

        // for constructs, put the soul shard in so it can immediately become one
        // if you transmute a sentient sheet of plasteel it will just be left on the floor next to the runed metal
        if (_container.TryGetContainer(args.Target, "Shard", out var container))
        {
            _container.Insert(shard, container, force: true);
        }
    }
}

/// <summary>
/// Event raised on the old entity when transmuting it, before the mind is transfered and it gets deleted.
/// </summary>
[ByRefEvent]
public record struct BloodCultTransmutedEvent(EntityUid Target, Entity<BloodCultRuleComponent> Rule, EntityCoordinates Position);
