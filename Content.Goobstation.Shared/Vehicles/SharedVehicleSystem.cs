// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Actions;
using Content.Shared.Audio;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Actions.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Trauma.Common.TileMovement;

namespace Content.Goobstation.Shared.Vehicles;

public abstract partial class SharedVehicleSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedBuckleSystem _buckle = default!;
    [Dependency] private SharedMoverController _mover = default!;
    [Dependency] private SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly EntProtoId HornActionId = "ActionHorn";
    private static readonly EntProtoId SirenActionId = "ActionSiren";

    [SubscribeLocalEvent]
    private void OnInit(EntityUid uid, VehicleComponent component, ComponentInit args)
    {
        _appearance.SetData(uid, VehicleState.Animated, component.EngineRunning);
        _appearance.SetData(uid, VehicleState.DrawOver, false);
    }

    [SubscribeLocalEvent]
    private void OnRemove(EntityUid uid, VehicleComponent component, ComponentRemove args)
    {
        if (component.Driver == null)
            return;

        _buckle.TryUnbuckle(component.Driver.Value, component.Driver.Value);
        Dismount(component.Driver.Value, uid);
        _appearance.SetData(uid, VehicleState.DrawOver, false);
    }

    [SubscribeLocalEvent]
    private void OnInsert(EntityUid uid, VehicleComponent component, ref EntInsertedIntoContainerMessage args)
    {
        if (HasComp<InstantActionComponent>(args.Entity)
            || args.Container.ID != component.KeySlot
            || component.IsBroken)
            return;

        component.EngineRunning = true;
        Dirty(uid, component);
        _appearance.SetData(uid, VehicleState.Animated, true);

        _ambientSound.SetAmbience(uid, true);

        if (component.Driver is { } driver)
            Mount(driver, uid);
    }

    [SubscribeLocalEvent]
    private void OnEject(EntityUid uid, VehicleComponent component, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != component.KeySlot)
            return;
        component.EngineRunning = false;
        Dirty(uid, component);
        _appearance.SetData(uid, VehicleState.Animated, false);
        _ambientSound.SetAmbience(uid, false);

        if (component.Driver is { } driver)
            Dismount(driver, uid);
    }

    [SubscribeLocalEvent]
    private void OnHorn(EntityUid uid, VehicleComponent component, HornActionEvent args)
    {
        var user = args.Performer;
        if (args.Handled || user != component.Driver || component.HornSound == null)
            return;

        _audio.PlayPredicted(component.HornSound, uid, user);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnSiren(EntityUid uid, VehicleComponent component, SirenActionEvent args)
    {
        var user = args.Performer;
        if (args.Handled || user != component.Driver || component.HornSound == null)
            return;

        component.SirenEnabled = !component.SirenEnabled;
        Dirty(uid, component);
        args.Handled = true;

        if (_net.IsClient)
            return; // PlayPredicted return value cant be stored it doesnt use PredictedSpawn

        component.SirenStream = component.SirenEnabled ? _audio.Stop(component.SirenStream) : _audio.PlayPvs(component.SirenSound, uid)?.Entity;
    }

    [SubscribeLocalEvent]
    private void OnBuckleAttempt(Entity<VehicleComponent> ent, ref BuckleAttemptEvent args)
    {
        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnStrapAttempt(Entity<VehicleComponent> ent, ref StrapAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        // no hotswapping drivers
        if (ent.Comp.Driver != null)
        {
            args.Cancelled = true;
            return;
        }

        var driver = args.Buckle.Owner;
        // if you have no hands available you cant drive it
        args.Cancelled = !TrySpawnVirtualItems(ent, driver);
    }

    private bool TrySpawnVirtualItems(Entity<VehicleComponent> ent, EntityUid driver)
    {
        if (ent.Comp.RequiredHands == 0)
            return true;

        _virtualItem.DeleteInHandsMatching(driver, ent.Owner, queueDel: false);
        for (var hands = 0; hands < ent.Comp.RequiredHands; hands++)
        {
            if (_virtualItem.TrySpawnVirtualItemInHand(ent.Owner, driver, false))
                continue;

            _virtualItem.DeleteInHandsMatching(driver, ent.Owner);
            return false;
        }

        return true;
    }

    [SubscribeLocalEvent]
    private void OnStrapped(Entity<VehicleComponent> ent, ref StrappedEvent args)
    {
        var driver = args.Buckle.Owner;

        if (!HasComp<MobMoverComponent>(driver) || ent.Comp.Driver != null)
            return;

        ent.Comp.Driver = driver;
        Dirty(ent);

        AddActions(ent, driver);
        _appearance.SetData(ent, VehicleState.DrawOver, true);

        SetupOverlay(ent);

        if (!ent.Comp.EngineRunning)
            return;

        Mount(driver, ent);
    }

    private void SetupOverlay(Entity<VehicleComponent> ent)
    {
        if (ent.Comp.OverlayPrototype is not {} proto)
            return;

        var overlay = PredictedSpawnAtPosition(proto, Transform(ent).Coordinates);
        _transform.SetParent(overlay, ent);
        _transform.SetLocalPosition(overlay, Vector2.Zero);
        _transform.SetLocalRotation(overlay, Angle.Zero);
        ent.Comp.ActiveOverlay = overlay;
    }

    [SubscribeLocalEvent]
    private void OnUnstrapped(Entity<VehicleComponent> ent, ref UnstrappedEvent args)
    {
        if (ent.Comp.Driver != args.Buckle.Owner)
            return;

        Dismount(args.Buckle.Owner, ent);
        _appearance.SetData(ent, VehicleState.DrawOver, false);
    }

    [SubscribeLocalEvent]
    private void OnDropped(Entity<VehicleComponent> ent, ref VirtualItemDeletedEvent args)
    {
        if (ent.Comp.Driver != args.User)
            return;

        _buckle.TryUnbuckle(args.User, args.User);
        Dismount(args.User, ent);
        _appearance.SetData(ent, VehicleState.DrawOver, false);
    }

    private void AddActions(Entity<VehicleComponent> ent, EntityUid driver)
    {
        if (ent.Comp.HornSound != null)
            _actions.AddAction(driver, HornActionId, ent.Owner);
        if (ent.Comp.SirenSound != null)
            _actions.AddAction(driver, SirenActionId, ent.Owner);
    }

    private void Mount(EntityUid driver, EntityUid vehicle)
    {
        _mover.SetRelay(driver, vehicle);

        if (HasComp<TileMovementComponent>(driver))
            EnsureComp<TileMovementComponent>(vehicle);

        var ev = new VehicleMountedEvent(driver);
        RaiseLocalEvent(vehicle, ref ev);
    }

    private void Dismount(EntityUid driver, EntityUid vehicle)
    {
        if (!TryComp<VehicleComponent>(vehicle, out var vehicleComp) || vehicleComp.Driver != driver)
            return;

        vehicleComp.Driver = null;
        Dirty(vehicle, vehicleComp);

        if (vehicleComp.ActiveOverlay is {} overlay)
        {
            PredictedQueueDel(overlay);
            vehicleComp.ActiveOverlay = null;
        }
        RemComp<RelayInputMoverComponent>(driver);

        _actions.RemoveProvidedActions(driver, vehicle);

        _virtualItem.DeleteInHandsMatching(driver, vehicle);

        RemComp<TileMovementComponent>(vehicle);

        var ev = new VehicleDismountedEvent(driver);
        RaiseLocalEvent(vehicle, ref ev);
    }

    [SubscribeLocalEvent]
    private void OnItemSlotEject(EntityUid uid, VehicleComponent comp, ref ItemSlotEjectAttemptEvent args)
    {
        if (!comp.PreventEjectOfKey || comp.Driver == null || args.Slot.ID != comp.KeySlot || args.User == comp.Driver)
            return;

        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnBreak(EntityUid uid, VehicleComponent component, BreakageEventArgs args)
    {
        component.IsBroken = true;

        // remove drivers ability to drive if there is a driver
        if (component.Driver is { } driver)
            Dismount(driver, uid);

        // stop animation
        component.EngineRunning = false;
        Dirty(uid, component);
        _appearance.SetData(uid, VehicleState.Animated, false);
        _ambientSound.SetAmbience(uid, false);
    }

// this is for repairing via bananas, rejuv or whatever else can do it. not damage dealt
#pragma warning disable CS0618
    [SubscribeLocalEvent]
    private void OnDamageChanged(Entity<VehicleComponent> ent, ref DamageChangedEvent args)
#pragma warning restore CS0618
    {
        if (!ent.Comp.IsBroken)
            return;

        var total = _damageable.GetTotalDamage(ent.Owner);
        if (total > FixedPoint2.Zero)
            return;

        ent.Comp.IsBroken = false;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnGetAdditionalAccess(Entity<VehicleComponent> ent, ref GetAdditionalAccessEvent args)
    {
        if (ent.Comp.Driver is { } driver)
            args.Entities.Add(driver);
    }
}

/// <summary>
/// Event raised on the vehicle after it can be driven (keys in and buckled)
/// </summary>
[ByRefEvent]
public record struct VehicleMountedEvent(EntityUid Driver);

/// <summary>
/// Event raised on the vehicle after it can no longer be driven (unbuckled, keys removed, etc)
/// </summary>
[ByRefEvent]
public record struct VehicleDismountedEvent(EntityUid Driver);
