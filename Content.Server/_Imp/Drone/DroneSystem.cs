// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Fishbait <Fishbait@git.ml>
// SPDX-FileCopyrightText: 2025 fishbait <gnesse@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Ghost.Roles.Components;
using Content.Server.Popups;
using Content.Server.Tools.Innate;
using Content.Shared.UserInterface;
using Content.Shared.Body.Components;
using Content.Shared._Imp.Drone;
using Content.Shared.Emoting;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Gibbing;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Timing;

namespace Content.Server._Imp.Drone;

// TODO: move 90% of this bullshit to shared
public sealed class DroneSystem : SharedDroneSystem
{
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InnateToolSystem _innateTool = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private HashSet<Entity<MindContainerComponent>> _mobs = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DroneComponent, UseAttemptEvent>(OnUseAttempt);
        SubscribeLocalEvent<DroneComponent, UserOpenActivatableUIAttemptEvent>(OnActivateUIAttempt);
        SubscribeLocalEvent<DroneComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<DroneComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DroneComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<DroneComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<DroneComponent, EmoteAttemptEvent>(OnEmoteAttempt);
        SubscribeLocalEvent<DroneComponent, ThrowAttemptEvent>(OnThrowAttempt);
    }

    // Imp. this replaces OnInteractionAttempt from the upstream version of DroneSystem.
    private void OnUseAttempt(EntityUid uid, DroneComponent component, UseAttemptEvent args)
    {
        if (NonDronesInRange(uid, component))
        {
            if (_whitelist.IsWhitelistPass(component.Blacklist, args.Used)) // imp special. blacklist. this one *does* prevent actions. it would probably be best if this read from the component or something.
            {
                args.Cancel();
                if (_timing.CurTime >= component.NextProximityAlert)
                {
                    _popup.PopupEntity(Loc.GetString("drone-cant-use-nearby", ("being", component.NearestEnt)), uid, uid);
                    component.NextProximityAlert = _timing.CurTime + component.ProximityDelay;
                }
            }

            else if (_whitelist.IsWhitelistPass(component.Whitelist, args.Used)) /// whitelist. sends proximity warning popup if the item isn't whitelisted. Doesn't prevent actions.
			{
                component.NextProximityAlert = _timing.CurTime + component.ProximityDelay;
                if (_timing.CurTime >= component.NextProximityAlert)
                {
                    _popup.PopupEntity(Loc.GetString("drone-too-close", ("being", component.NearestEnt)), uid, uid);
                    component.NextProximityAlert = _timing.CurTime + component.ProximityDelay;
                }
            }
        }
        else if (_whitelist.IsWhitelistPass(component.Blacklist, args.Used))
        {
            args.Cancel();
            if (_timing.CurTime >= component.NextProximityAlert)
            {
                _popup.PopupEntity(Loc.GetString("drone-cant-use"), uid, uid);
                component.NextProximityAlert = _timing.CurTime + component.ProximityDelay;
            }
        }
    }

    private void OnActivateUIAttempt(EntityUid uid, DroneComponent component, UserOpenActivatableUIAttemptEvent args)
    {
        if (_whitelist.IsWhitelistPass(component.Blacklist, args.Target))
            args.Cancel();
    }

    private void OnExamined(EntityUid uid, DroneComponent component, ExaminedEvent args)
    {
        if (TryComp<MindContainerComponent>(uid, out var mind) && mind.HasMind)
        {
            args.PushMarkup(Loc.GetString("drone-active"));
        }
        else
        {
            args.PushMarkup(Loc.GetString("drone-dormant"));
        }
    }

    private void OnMobStateChanged(EntityUid uid, DroneComponent drone, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (TryComp<InnateToolComponent>(uid, out var innate))
            _innateTool.Cleanup(uid, innate);

        _gibbing.Gib(uid);
        QueueDel(uid);
    }

    private void OnMindAdded(EntityUid uid, DroneComponent drone, MindAddedMessage args)
    {
        UpdateDroneAppearance(uid, DroneStatus.On);
        _popup.PopupEntity(Loc.GetString("drone-activated"), uid, PopupType.Large);
    }

    private void OnMindRemoved(EntityUid uid, DroneComponent drone, MindRemovedMessage args)
    {
        UpdateDroneAppearance(uid, DroneStatus.Off);
        EnsureComp<GhostTakeoverAvailableComponent>(uid);
    }

    private void OnEmoteAttempt(EntityUid uid, DroneComponent component, EmoteAttemptEvent args)
    {
        // No.
        args.Cancel();
    }

    private void OnThrowAttempt(EntityUid uid, DroneComponent drone, ThrowAttemptEvent args)
    {
        args.Cancel();
    }

    private void UpdateDroneAppearance(EntityUid uid, DroneStatus status)
    {
        _appearance.SetData(uid, DroneVisuals.Status, status);
    }

    private bool NonDronesInRange(EntityUid uid, DroneComponent component)
    {
        var xform = Transform(uid);
        _mobs.Clear();
        _lookup.GetEntitiesInRange(xform.MapPosition, component.InteractionBlockRange, _mobs);
        foreach (var entity in _mobs)
        {
            // Require the entity to be controlled by a player and not a drone or ghost.
            if (!entity.Comp.HasMind || HasComp<DroneComponent>(entity) || HasComp<GhostComponent>(entity))
                continue;

            // filter out all dead entities.
            if (_mob.IsDead(entity.Owner))
                continue;

            // instead of doing popups in here, set a variable to the nearest entity for use elsewhere.
            component.NearestEnt = Identity.Entity(entity, EntityManager);
            return true;
        }

        return false;
    }
}
