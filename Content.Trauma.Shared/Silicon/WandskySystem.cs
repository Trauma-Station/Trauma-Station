// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Trauma.Common.Silicon;
using Content.Trauma.Shared.Silicon.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Silicon;
public sealed partial class WandskySystem : EntitySystem
{

    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlaveComponent, InteractUsingEvent>(OnGetSlave);

        SubscribeLocalEvent<CommanderComponent, TogglePatrolActionEvent>(OnTogglePatrol);
        SubscribeLocalEvent<CommanderComponent, WaypointActionEvent>(OnWaypointAction);
        SubscribeLocalEvent<CommanderComponent, ClearWaypointsActionEvent>(OnClearWaypoints);
    }

    #region CommanderEvents

    public void OnGetSlave(Entity<SlaveComponent> ent, ref InteractUsingEvent args)
    {
        if (!TryComp<CommanderComponent>(args.Used, out var commander))
            return;

        if (ent.Comp.MasterEntity is { } && commander.SlaveEntity == ent.Owner)
        {
            _popupSystem.PopupClient("A bond has already been formed with this one.", ent.Owner, args.User, PopupType.Medium);
            return;
        }

        _popupSystem.PopupClient("Bond formed.", ent.Owner, args.User, PopupType.Medium);

        var slaveEntity = ent.Owner;

        // Clear old one
        if (TryComp<SlaveComponent>(commander.SlaveEntity, out var slave))
        {
            if (TryComp<CommanderComponent>(slave.MasterEntity, out var master))
                master.SlaveEntity = null;
            slave.MasterEntity = null;
        }

        // Set new one
        commander.SlaveEntity = slaveEntity;
        ent.Comp.MasterEntity = ent.Owner;

        Dirty(ent);
        Dirty(args.Used, commander);

        _audio.PlayPvs(commander.EnslaveSound, ent);
    }

    public void OnTogglePatrol(Entity<CommanderComponent> ent, ref TogglePatrolActionEvent args)
    {
        if (ent.Comp.SlaveEntity is not { } || !TryComp<SlaveComponent>(ent.Comp.SlaveEntity, out var slave))
        {
            _popupSystem.PopupClient("You have not synced to a Securitron", ent.Owner, args.Performer, PopupType.Medium);
            return;
        }

        slave.IsPatrolling = !slave.IsPatrolling;

        var message = slave.IsPatrolling ? "PATROL ENABLED!" : "PATROL DISABLED!";

        Dirty(ent);
        _popupSystem.PopupClient(message, ent.Owner, args.Performer, PopupType.Medium);
    }

    public void OnWaypointAction(Entity<CommanderComponent> ent, ref WaypointActionEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var items = _lookup.GetEntitiesInRange(args.Target, 0.5f, LookupFlags.All);

        if (items is { } targetEntities)
        {
            foreach (var targetEntity in targetEntities)
            {
                if (!HasComp<WaypointComponent>(targetEntity))
                    continue;

                if (!ent.Comp.Waypoints.Contains(targetEntity))
                    return;

                _popupSystem.PopupClient("Waypoint removed!", args.Performer, args.Performer, PopupType.Medium);

                if (_net.IsServer)
                {
                    ent.Comp.Waypoints.Remove(targetEntity);
                    QueueDel(targetEntity);
                }
                Dirty(ent);
                return;
            }
        }

        if (_net.IsServer)
        {
            var waypointEntity = Spawn(ent.Comp.WaypointEntityUid, args.Target);
            ent.Comp.Waypoints.Add(waypointEntity);
        }
        _popupSystem.PopupClient("Waypoint added!", args.Performer, args.Performer, PopupType.Medium);
        Dirty(ent);
    }

    public void OnClearWaypoints(Entity<CommanderComponent> ent, ref ClearWaypointsActionEvent args)
    {
        var waypoints = ent.Comp.Waypoints;
        var count = waypoints.Count;

        if (count == 0)
        {
            _popupSystem.PopupClient("No waypoints to clear!", ent.Owner, args.Performer, PopupType.Medium);
            return;
        }
        if (_net.IsServer)
        {
            waypoints.RemoveWhere(waypoint =>
            {
                if (!Exists(waypoint))
                    return false;
                QueueDel(waypoint);
                return true;
            });
        }
        Dirty(ent);
        _popupSystem.PopupClient($"Cleared {count} waypoints!", ent.Owner, args.Performer, PopupType.Medium);
    }
    #endregion
}
