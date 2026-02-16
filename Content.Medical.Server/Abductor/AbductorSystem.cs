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
        Subs.BuiEvents<AbductorHumanObservationConsoleComponent>(AbductorCameraConsoleUIKey.Key, subs => subs.Event<AbductorBeaconChosenBuiMsg>(OnAbductorBeaconChosenBuiMsg));

        InitializeActions();
        InitializeConsole();
        InitializeVictim();
    }

    private void OnAbductorBeaconChosenBuiMsg(Entity<AbductorHumanObservationConsoleComponent> ent, ref AbductorBeaconChosenBuiMsg args)
    {
        OnCameraExit(args.Actor);

        var beacon = GetEntity(args.Beacon.NetEnt);
        var eye = SpawnAtPosition(ent.Comp.RemoteEntityProto, Transform(beacon).Coordinates);
        ent.Comp.RemoteEntity = GetNetEntity(eye);

        // TODO: holy shitcode just disable interaction??????
        if (TryComp<HandsComponent>(args.Actor, out var hands))
        {
            foreach (var hand in _hands.EnumerateHands((args.Actor, hands)))
            {
                if (!_hands.TryGetHeldItem((args.Actor, hands), hand, out var held))
                    continue;

                if (HasComp<UnremoveableComponent>(held))
                    continue;

                _hands.DoDrop((args.Actor, hands), hand);
            }

            if (_virtualItem.TrySpawnVirtualItemInHand(ent.Owner, args.Actor, out var virtItem1))
                EnsureComp<UnremoveableComponent>(virtItem1.Value);

            if (_virtualItem.TrySpawnVirtualItemInHand(ent.Owner, args.Actor, out var virtItem2))
                EnsureComp<UnremoveableComponent>(virtItem2.Value);
        }

        var visibility = EnsureComp<VisibilityComponent>(eye);

        Dirty(ent);

        if (TryComp(args.Actor, out EyeComponent? eyeComp))
        {
            _eye.SetVisibilityMask(args.Actor, eyeComp.VisibilityMask | (int) VisibilityFlags.Abductor, eyeComp);
            _eye.SetTarget(args.Actor, eye, eyeComp);
            _eye.SetDrawFov(args.Actor, false);
            _eye.SetRotation(args.Actor, Angle.Zero, eyeComp);
            Dirty(args.Actor, eyeComp);
            var overlay = EnsureComp<StationAiOverlayComponent>(args.Actor);
            overlay.AllowCrossGrid = true;
            Dirty(args.Actor, overlay);
            var remote = EnsureComp<RemoteEyeSourceContainerComponent>(eye);
            remote.Actor = args.Actor;
            Dirty(eye, remote);
        }

        AddActions(args);

        _mover.SetRelay(args.Actor, eye);
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
