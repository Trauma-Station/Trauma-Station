// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Trauma.Shared.Silicon.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Silicon;

/// <summary>
/// Used by wandsky to assign secbots waypoints for patrol paths.
/// </summary>
public sealed partial class WandskySystem : EntitySystem
{

    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IGameTiming _timing = default!;

    private HashSet<Entity<WaypointComponent>> _waypoints = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PatrolSlaveComponent, InteractUsingEvent>(OnInteractUsing);

        SubscribeLocalEvent<PatrolCommanderComponent, TogglePatrolActionEvent>(OnTogglePatrol);
        SubscribeLocalEvent<PatrolCommanderComponent, WaypointActionEvent>(OnWaypointAction);
        SubscribeLocalEvent<PatrolCommanderComponent, ClearWaypointsActionEvent>(OnClearWaypoints);
    }

    public void OnInteractUsing(Entity<PatrolSlaveComponent> ent, ref InteractUsingEvent args)
    {
        if (!TryComp<PatrolCommanderComponent>(args.Used, out var commander))
            return;

        if (ent.Comp.MasterEntity == args.Used)
        {
            _popup.PopupClient("Bond deleted.", ent.Owner, args.User, PopupType.Medium);
            ent.Comp.MasterEntity = null;
            Dirty(ent);
            return;
        }

        _popup.PopupClient("Bond formed.", ent.Owner, args.User, PopupType.Medium);

        ent.Comp.MasterEntity = args.Used;

        Dirty(ent);
        _audio.PlayPredicted(commander.EnslaveSound, ent, args.User);
    }

    public void OnTogglePatrol(Entity<PatrolCommanderComponent> ent, ref TogglePatrolActionEvent args)
    {
        ent.Comp.IsPatrolling = !ent.Comp.IsPatrolling;

        var message = ent.Comp.IsPatrolling ? "PATROL ENABLED!" : "PATROL DISABLED!";

        Dirty(ent);
        _popup.PopupClient(message, ent.Owner, args.Performer, PopupType.Medium);
    }

    public void OnWaypointAction(Entity<PatrolCommanderComponent> ent, ref WaypointActionEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        _waypoints.Clear();
        _lookup.GetEntitiesInRange(args.Target, 0.5f, _waypoints);

        foreach (var waypoint in _waypoints)
        {
            if (!ent.Comp.Waypoints.Contains(waypoint))
                return;

            _popup.PopupClient("Waypoint removed!", args.Performer, args.Performer, PopupType.Medium);

            ent.Comp.Waypoints.Remove(waypoint);
            PredictedQueueDel(waypoint);
            Dirty(ent);
            return;
        }

        _popup.PopupClient("Waypoint added!", args.Performer, args.Performer, PopupType.Medium);
        var waypointEntity = PredictedSpawnAtPosition(ent.Comp.WaypointId, args.Target);
        ent.Comp.Waypoints.Add(waypointEntity);
        Dirty(ent);
    }

    public void OnClearWaypoints(Entity<PatrolCommanderComponent> ent, ref ClearWaypointsActionEvent args)
    {
        var waypoints = ent.Comp.Waypoints;
        var count = waypoints.Count;

        if (count == 0)
        {
            _popup.PopupClient("No waypoints to clear!", ent.Owner, args.Performer, PopupType.Medium);
            return;
        }

        _popup.PopupClient($"Cleared {count} waypoints!", ent.Owner, args.Performer, PopupType.Medium);

        foreach (var waypoint in waypoints)
        {
            PredictedQueueDel(waypoint);
        }
        ent.Comp.Waypoints.Clear();
        Dirty(ent);
    }
}
