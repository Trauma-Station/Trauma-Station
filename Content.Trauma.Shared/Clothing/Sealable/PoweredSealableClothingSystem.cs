// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Alert;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Wires;

namespace Content.Trauma.Shared.Clothing.Sealable;

/// <summary>
/// Used for sealable clothing that requires power to work, aka modsuits.
/// </summary>
public sealed partial class PoweredSealableClothingSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityQuery<SealableClothingControlComponent> _controlQuery = default!;
    [Dependency] private EntityQuery<PowerCellDrawComponent> _powerDrawQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.SubscribeWithRelay<SealableClothingRequiresPowerComponent, FindBatteryEvent>(OnFindBattery, held: false);
    }

    [SubscribeLocalEvent]
    private void OnRequiresPowerMapInit(Entity<SealableClothingRequiresPowerComponent> ent, ref MapInitEvent args)
    {
        if (!_controlQuery.TryComp(ent, out var control) || !_powerDrawQuery.TryComp(ent, out var draw))
            return;

        draw.Enabled = control.IsCurrentlySealed;
        Dirty(ent, draw);
    }

    /// <summary>
    /// Checks if control have enough power to seal
    /// </summary>
    [SubscribeLocalEvent]
    private void OnRequiresPowerSealAttempt(Entity<SealableClothingRequiresPowerComponent> ent, ref ClothingSealAttemptEvent args)
    {
        if (!TryComp(ent, out SealableClothingControlComponent? controlComp) || !TryComp(ent, out PowerCellDrawComponent? cellDrawComp) || args.Cancelled)
            return;

        // Prevents sealing if wires panel is opened
        if (TryComp(ent, out WiresPanelComponent? panel) && panel.Open)
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.ClosePanelFirstPopup), ent, args.User);
            args.Cancelled = true;
            return;
        }

        // Control shouldn't use charge on unsealing
        if (controlComp.IsCurrentlySealed)
            return;

        if (!_powerCell.HasDrawCharge((ent.Owner, cellDrawComp)) || !_powerCell.HasActivatableCharge((ent.Owner, cellDrawComp)))
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.NotPoweredPopup), ent, args.User);
            args.Cancelled = true;
        }
    }

    /// <summary>
    /// Prevents wires panel from opening if clothing is sealed
    /// </summary>
    [SubscribeLocalEvent]
    private void OnRequiresPowerChangePanelAttempt(Entity<SealableClothingRequiresPowerComponent> ent, ref AttemptChangePanelEvent args)
    {
        if (args.Cancelled || !_controlQuery.TryComp(ent, out var controlComp))
            return;

        if (controlComp.IsCurrentlySealed || controlComp.IsInProcess)
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.OpenSealedPanelFailPopup), ent, args.User);
            args.Cancelled = true;
        }
    }

    [SubscribeLocalEvent]
    private void OnPowerCellChanged(Entity<SealableClothingRequiresPowerComponent> ent, ref PowerCellChangedEvent args)
    {
        if (!ent.Comp.IsPowered && _powerCell.HasDrawCharge(ent.Owner))
        {
            ent.Comp.IsPowered = true;
            Dirty(ent);

            ModifySpeed(ent);
        }

        UpdateClothingPowerAlert(ent);
    }

    [SubscribeLocalEvent]
    private void OnPowerCellEmpty(Entity<SealableClothingRequiresPowerComponent> ent, ref PowerCellSlotEmptyEvent args)
    {
        ent.Comp.IsPowered = false;
        Dirty(ent);

        ModifySpeed(ent);
    }

    /// <summary>
    /// Enables or disables power cell draw on seal/unseal complete
    /// </summary>
    [SubscribeLocalEvent]
    private void OnSealComplete(Entity<SealableClothingRequiresPowerComponent> ent, ref ClothingControlSealCompleteEvent args)
    {
        if (!_powerDrawQuery.TryComp(ent, out var drawComp))
            return;

        _powerCell.SetDrawEnabled((ent.Owner, drawComp), args.IsSealed);

        UpdateClothingPowerAlert(ent);
        ModifySpeed(ent);
    }

    [SubscribeLocalEvent]
    private void OnMovementSpeedChange(Entity<SealableClothingRequiresPowerComponent> ent, ref InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        if (!_controlQuery.TryComp(ent, out var controlComp))
            return;

        // If suit is unsealed - don't care about penalty
        if (!controlComp.IsCurrentlySealed)
            return;

        if (!ent.Comp.IsPowered)
            args.Args.ModifySpeed(ent.Comp.MovementSpeedPenalty);
    }

    private void ModifySpeed(EntityUid uid)
    {
        _movement.RefreshMovementSpeedModifiers(Transform(uid).ParentUid);
    }

    /// <summary>
    /// Sets power alert to wearer when clothing is sealed
    /// </summary>
    [SubscribeLocalEvent]
    private void UpdateClothingPowerAlert(Entity<SealableClothingRequiresPowerComponent> ent)
    {
        if (!_controlQuery.TryComp(ent, out var controlComp))
            return;

        var wearer = Transform(ent).ParentUid;

        if (!TryComp(wearer, out AlertsComponent? alerts))
            return;

        if (!_powerCell.TryGetBatteryFromSlot(ent.Owner, out var battery) || !controlComp.IsCurrentlySealed)
        {
            _alerts.ClearAlert((wearer, alerts), ent.Comp.SuitPowerAlert);
            return;
        }

        var severity = _battery.GetRemainingUses(battery.Value.AsNullable(), battery.Value.Comp.MaxCharge / 5f);
        _alerts.ShowAlert((wearer, alerts), ent.Comp.SuitPowerAlert, (short) severity);
    }

    /// <summary>
    /// Tries to find battery for charger
    /// </summary>
    private void OnFindBattery(Entity<SealableClothingRequiresPowerComponent> ent, ref FindBatteryEvent args)
    {
        if (args.FoundBattery != null)
            return;

        if (_powerCell.TryGetBatteryFromSlot(ent.Owner, out var battery))
            args.FoundBattery = battery;
    }
}
