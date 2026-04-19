// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Alert;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Drone;
using Content.Shared.Movement.Systems;
using Content.Trauma.Common.Body;
using Content.Trauma.Common.Silicon;
using Content.Trauma.Shared.Silicon.Components;

namespace Content.Trauma.Shared.Silicon.Systems;

public sealed class SharedSiliconChargeSystem : CommonSiliconSystem
{
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;

    private static readonly ProtoId<DamageTypePrototype> IonDamageType = "Ion";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconComponent, ComponentInit>(OnSiliconInit);
        SubscribeLocalEvent<SiliconComponent, SiliconChargeStateUpdateEvent>(OnSiliconChargeStateUpdate);
        SubscribeLocalEvent<SiliconComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        // Monolith - IPC Rework
        /*
        SubscribeLocalEvent<SiliconComponent, ItemSlotInsertAttemptEvent>(OnItemSlotInsertAttempt);
        SubscribeLocalEvent<SiliconComponent, ItemSlotEjectAttemptEvent>(OnItemSlotEjectAttempt);
        */
        SubscribeLocalEvent<SiliconComponent, TryingToSleepEvent>(OnTryingToSleep);
        SubscribeLocalEvent<SiliconComponent, SuicideDamageEvent>(OnSuicide);
    }

    // Monolith - IPC Rework
    /*
    private void OnItemSlotInsertAttempt(EntityUid uid, SiliconComponent component, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Cancelled
            || !TryComp<PowerCellSlotComponent>(uid, out var cellSlotComp)
            || !_itemSlots.TryGetSlot(uid, cellSlotComp.CellSlotId, out var cellSlot)
            || cellSlot != args.Slot || args.User != uid)
            return;

        args.Cancelled = true;
    }

    private void OnItemSlotEjectAttempt(EntityUid uid, SiliconComponent component, ref ItemSlotEjectAttemptEvent args)
    {
        if (args.Cancelled
            || !TryComp<PowerCellSlotComponent>(uid, out var cellSlotComp)
            || !_itemSlots.TryGetSlot(uid, cellSlotComp.CellSlotId, out var cellSlot)
            || cellSlot != args.Slot || args.User != uid)
            return;

        args.Cancelled = true;
    }
    */

    private void OnSiliconInit(EntityUid uid, SiliconComponent component, ComponentInit args)
    {
        if (!component.BatteryPowered)
            return;

        _alertsSystem.ShowAlert(uid, component.BatteryAlert, component.ChargeState);
    }

    private void OnSiliconChargeStateUpdate(EntityUid uid, SiliconComponent component, SiliconChargeStateUpdateEvent ev)
    {
        _alertsSystem.ShowAlert(uid, component.BatteryAlert, ev.ChargePercent);
    }

    private void OnRefreshMovespeed(EntityUid uid, SiliconComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (!component.BatteryPowered)
            return;

        var closest = 0;

        foreach (var state in component.SpeedModifierThresholds)
            if (component.ChargeState >= state.Key && state.Key > closest)
                closest = state.Key;

        var speedMod = component.SpeedModifierThresholds[closest];

        args.ModifySpeed(speedMod, speedMod);
    }

    /// <summary>
    ///     Silicon entities can now also be Living player entities. We may want to prevent them from sleeping if they can't sleep.
    /// </summary>
    private void OnTryingToSleep(Entity<SiliconComponent> ent, ref TryingToSleepEvent args)
    {
        args.Cancelled = !ent.Comp.DoSiliconsDreamOfElectricSheep;
    }

    private void OnSuicide(Entity<SiliconComponent> ent, ref SuicideDamageEvent args)
    {
        args.DamageType = IonDamageType;
    }

    public override bool IsSilicon(EntityUid uid)
    {
        return HasComp<SiliconComponent>(uid);
    }

    public override bool IsDrone(EntityUid uid)
    {
        return HasComp<DroneComponent>(uid);
    }
}


public enum SiliconType
{
    Player,
    GhostRole,
    Npc,
}

/// <summary>
///     Event raised when a Silicon's charge state needs to be updated.
/// </summary>
[Serializable, NetSerializable]
public sealed class SiliconChargeStateUpdateEvent : EntityEventArgs
{
    public short ChargePercent { get; }

    public SiliconChargeStateUpdateEvent(short chargePercent)
    {
        ChargePercent = chargePercent;
    }
}
