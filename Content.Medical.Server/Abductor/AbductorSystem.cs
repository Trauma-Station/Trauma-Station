// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Abductor;
using Content.Shared.Actions;
using Content.Shared.Eye;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Pinpointer;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Interaction.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Station;
using Content.Shared.Station.Components;
using Content.Shared.UserInterface;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Containers;

namespace Content.Medical.Server.Abductor;

public sealed partial class AbductorSystem : SharedAbductorSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AbductorHumanObservationConsoleComponent, BeforeActivatableUIOpenEvent>(OnBeforeActivatableUIOpen);
        SubscribeLocalEvent<AbductorHumanObservationConsoleComponent, ActivatableUIOpenAttemptEvent>(OnActivatableUIOpenAttempt);
        Subs.BuiEvents<AbductorHumanObservationConsoleComponent>(AbductorCameraConsoleUIKey.Key, subs =>
        {
            subs.Event<AbductorBeaconChosenBuiMsg>(OnAbductorBeaconChosenBuiMsg);
        });

        InitializeActions();
        InitializeConsole();
        InitializeVictim();
    }

    private void OnAbductorBeaconChosenBuiMsg(Entity<AbductorHumanObservationConsoleComponent> ent, ref AbductorBeaconChosenBuiMsg args)
    {
        var user = args.Actor;
        OnCameraExit(user);

        var beacon = GetEntity(args.Target);
        if (!HasComp<NavMapBeaconComponent>(beacon))
            return; // malf client trying to teleport to arbitrary entities

        var xform = Transform(beacon);
        if (xform.MapID != Transform(ent).MapID)
        {
            _popup.PopupEntity(Loc.GetString("abductor-console-ftl-to-station"), user, user);
            return;
        }

        var eye = SpawnAtPosition(ent.Comp.RemoteEntityProto, xform.Coordinates);

        // TODO: holy shitcode just disable interaction??????
        if (TryComp<HandsComponent>(user, out var hands))
        {
            foreach (var hand in _hands.EnumerateHands((user, hands)))
            {
                if (!_hands.TryGetHeldItem((user, hands), hand, out var held))
                    continue;

                if (HasComp<UnremoveableComponent>(held))
                    continue;

                _hands.DoDrop((user, hands), hand);
            }

            if (_virtualItem.TrySpawnVirtualItemInHand(ent.Owner, user, out var virtItem1))
                EnsureComp<UnremoveableComponent>(virtItem1.Value);

            if (_virtualItem.TrySpawnVirtualItemInHand(ent.Owner, user, out var virtItem2))
                EnsureComp<UnremoveableComponent>(virtItem2.Value);
        }

        var visibility = EnsureComp<VisibilityComponent>(eye);

        if (TryComp(user, out EyeComponent? eyeComp))
        {
            _eye.SetVisibilityMask(user, eyeComp.VisibilityMask | (int) VisibilityFlags.Abductor, eyeComp);
            _eye.SetTarget(user, eye, eyeComp);
            _eye.SetDrawFov(user, false);
            _eye.SetRotation(user, Angle.Zero, eyeComp);
            Dirty(user, eyeComp);
            var overlay = EnsureComp<StationAiOverlayComponent>(user);
            overlay.AllowCrossGrid = true;
            Dirty(user, overlay);
            var remote = EnsureComp<RemoteEyeSourceContainerComponent>(eye);
            remote.Actor = user;
            Dirty(eye, remote);
        }

        AddActions(user);

        _mover.SetRelay(user, eye);
    }

    private void OnCameraExit(EntityUid actor)
    {
        if (!TryComp<RelayInputMoverComponent>(actor, out var comp) ||
            !TryComp<AbductorScientistComponent>(actor, out var abductorComp))
            return; // lol lmao

        var relay = comp.RelayEntity;
        RemComp(actor, comp);

        if (abductorComp.Console is {} console)
            _virtualItem.DeleteInHandsMatching(actor, console);

        RemComp<StationAiOverlayComponent>(actor);
        if (TryComp(actor, out EyeComponent? eyeComp))
        {
            _eye.SetVisibilityMask(actor, eyeComp.VisibilityMask ^ (int) VisibilityFlags.Abductor, eyeComp);
            _eye.SetDrawFov(actor, true);
            _eye.SetTarget(actor, null, eyeComp);
        }
        RemoveActions(actor);
        QueueDel(relay);
    }

    private void OnBeforeActivatableUIOpen(Entity<AbductorHumanObservationConsoleComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        if (!TryComp<AbductorScientistComponent>(args.User, out var abductorComp))
            return;

        abductorComp.Console = ent.Owner;
        var stations = _station.GetStations();
        var result = new Dictionary<int, StationBeacons>();

        foreach (var station in stations)
        {
            if (_station.GetLargestGrid(station) is not { } grid)
                return;

            if (!TryComp<NavMapComponent>(grid, out var navMap))
                return;

            result.Add(station.Id, new StationBeacons
            {
                Name = Name(station),
                StationId = station.Id,
                Beacons = [.. navMap.Beacons.Values],
            });
        }

        _ui.SetUiState(ent.Owner, AbductorCameraConsoleUIKey.Key, new AbductorCameraConsoleBuiState() { Stations = result });
    }

    private void OnActivatableUIOpenAttempt(Entity<AbductorHumanObservationConsoleComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (!HasComp<AbductorScientistComponent>(args.User))
            args.Cancel();
    }

}
