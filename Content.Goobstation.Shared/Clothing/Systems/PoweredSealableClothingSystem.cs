// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Clothing.Components;
using Content.Shared.Alert;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Rounding;
using Content.Shared.Wires;

namespace Content.Goobstation.Shared.Clothing.Systems;

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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SealableClothingRequiresPowerComponent, MapInitEvent>(OnRequiresPowerMapInit);
        SubscribeLocalEvent<SealableClothingRequiresPowerComponent, ClothingSealAttemptEvent>(OnRequiresPowerSealAttempt);
        SubscribeLocalEvent<SealableClothingRequiresPowerComponent, AttemptChangePanelEvent>(OnRequiresPowerChangePanelAttempt);
        SubscribeLocalEvent<SealableClothingRequiresPowerComponent, InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnMovementSpeedChange);
        SubscribeLocalEvent<SealableClothingRequiresPowerComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<SealableClothingRequiresPowerComponent, PowerCellSlotEmptyEvent>(OnPowerCellEmpty);
        SubscribeLocalEvent<SealableClothingRequiresPowerComponent, ClothingControlSealCompleteEvent>(OnSealComplete);
        Subs.SubscribeWithRelay<SealableClothingRequiresPowerComponent, FindBatteryEvent>(OnFindBattery, held: false);
    }

    private void OnRequiresPowerMapInit(Entity<SealableClothingRequiresPowerComponent> entity, ref MapInitEvent args)
    {
        if (!TryComp(entity, out SealableClothingControlComponent? control) || !TryComp(entity, out PowerCellDrawComponent? draw))
            return;

        draw.Enabled = control.IsCurrentlySealed;
    }

    /// <summary>
    /// Checks if control have enough power to seal
    /// </summary>
    private void OnRequiresPowerSealAttempt(Entity<SealableClothingRequiresPowerComponent> entity, ref ClothingSealAttemptEvent args)
    {
        if (!TryComp(entity, out SealableClothingControlComponent? controlComp) || !TryComp(entity, out PowerCellDrawComponent? cellDrawComp) || args.Cancelled)
            return;

        // Prevents sealing if wires panel is opened
        if (TryComp(entity, out WiresPanelComponent? panel) && panel.Open)
        {
            _popup.PopupClient(Loc.GetString(entity.Comp.ClosePanelFirstPopup), entity, args.User);
            args.Cancel();
            return;
        }

        // Control shouldn't use charge on unsealing
        if (controlComp.IsCurrentlySealed)
            return;

        var ent = (entity.Owner, cellDrawComp);
        if (!_powerCell.HasDrawCharge(ent) || !_powerCell.HasActivatableCharge(ent))
        {
            _popup.PopupClient(Loc.GetString(entity.Comp.NotPoweredPopup), entity, args.User);
            args.Cancel();
        }
    }

    /// <summary>
    /// Prevents wires panel from opening if clothing is sealed
    /// </summary>
    private void OnRequiresPowerChangePanelAttempt(Entity<SealableClothingRequiresPowerComponent> entity, ref AttemptChangePanelEvent args)
    {
        if (args.Cancelled || !TryComp(entity, out SealableClothingControlComponent? controlComp))
            return;

        if (controlComp.IsCurrentlySealed || controlComp.IsInProcess)
        {
            _popup.PopupClient(Loc.GetString(entity.Comp.OpenSealedPanelFailPopup), entity, args.User);
            args.Cancelled = true;
        }
    }

    private void OnPowerCellChanged(Entity<SealableClothingRequiresPowerComponent> entity, ref PowerCellChangedEvent args)
    {
        if (!entity.Comp.IsPowered && _powerCell.HasDrawCharge(entity.Owner))
        {
            entity.Comp.IsPowered = true;
            Dirty(entity);

            ModifySpeed(entity);
        }

        UpdateClothingPowerAlert(entity);
    }

    private void OnPowerCellEmpty(Entity<SealableClothingRequiresPowerComponent> entity, ref PowerCellSlotEmptyEvent args)
    {
        entity.Comp.IsPowered = false;
        Dirty(entity);

        ModifySpeed(entity);
    }

    /// <summary>
    /// Enables or disables power cell draw on seal/unseal complete
    /// </summary>
    private void OnSealComplete(Entity<SealableClothingRequiresPowerComponent> entity, ref ClothingControlSealCompleteEvent args)
    {
        if (!TryComp(entity, out PowerCellDrawComponent? drawComp))
            return;

        _powerCell.SetDrawEnabled((entity.Owner, drawComp), args.IsSealed);

        UpdateClothingPowerAlert(entity);
        ModifySpeed(entity);
    }

    private void OnMovementSpeedChange(Entity<SealableClothingRequiresPowerComponent> entity, ref InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        if (!TryComp(entity, out SealableClothingControlComponent? controlComp))
            return;

        // If suit is unsealed - don't care about penalty
        if (!controlComp.IsCurrentlySealed)
            return;

        if (!entity.Comp.IsPowered)
            args.Args.ModifySpeed(entity.Comp.MovementSpeedPenalty);
    }

    private void ModifySpeed(EntityUid uid)
    {
        if (!TryComp(uid, out SealableClothingControlComponent? controlComp) || controlComp.WearerEntity == null)
            return;

        _movement.RefreshMovementSpeedModifiers(controlComp.WearerEntity.Value);
    }

    /// <summary>
    /// Sets power alert to wearer when clothing is sealed
    /// </summary>
    private void UpdateClothingPowerAlert(Entity<SealableClothingRequiresPowerComponent> entity)
    {
        var (uid, comp) = entity;

        if (!TryComp<SealableClothingControlComponent>(uid, out var controlComp) || controlComp.WearerEntity == null)
            return;

        if (!_powerCell.TryGetBatteryFromSlot(entity.Owner, out var battery) || !controlComp.IsCurrentlySealed)
        {
            _alerts.ClearAlert(controlComp.WearerEntity.Value, comp.SuitPowerAlert);
            return;
        }

        var severity = _battery.GetRemainingUses(battery.Value.AsNullable(), battery.Value.Comp.MaxCharge / 5f);
        _alerts.ShowAlert(controlComp.WearerEntity.Value, comp.SuitPowerAlert, (short) severity);
    }

    /// <summary>
    /// Tries to find battery for charger
    /// </summary>
    private void OnFindBattery(Entity<SealableClothingRequiresPowerComponent> entity, ref FindBatteryEvent args)
    {
        if (args.FoundBattery != null)
            return;

        if (_powerCell.TryGetBatteryFromSlot(entity.Owner, out var battery))
            args.FoundBattery = battery;
    }
}
