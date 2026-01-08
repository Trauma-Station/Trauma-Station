// SPDX-FileCopyrightText: 2024 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Spatison <137375981+Spatison@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 BombasterDS <deniskaporoshok@gmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 SX-7 <sn1.test.preria.2002@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._White.Blocking;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;

namespace Content.Server._White.Blocking;

// TODO: move this to shared goobmod and predict
public sealed class RechargeableBlockingSystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RechargeableBlockingComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RechargeableBlockingComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<RechargeableBlockingComponent, ItemToggleActivateAttemptEvent>(AttemptToggle);
        SubscribeLocalEvent<RechargeableBlockingComponent, ChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<RechargeableBlockingComponent, PowerCellChangedEvent>(OnPowerCellChanged);
    }

    private void OnExamined(EntityUid uid, RechargeableBlockingComponent component, ExaminedEvent args)
    {
        if (!component.Discharged)
            return;

        args.PushMarkup(Loc.GetString("rechargeable-blocking-discharged"));
        args.PushMarkup(Loc.GetString("rechargeable-blocking-remaining-time", ("remainingTime", GetRemainingTime(uid))));
    }

    private int GetRemainingTime(EntityUid uid)
    {
        if (_battery.GetBattery(uid) is not {} battery || battery.Comp.ChargeRate == 0)
            return 0;

        var remaining = battery.Comp.MaxCharge - _battery.GetCharge(battery.AsNullable());
        return (int) MathF.Round(remaining / battery.Comp.ChargeRate);
    }

    private void OnDamageChanged(EntityUid uid, RechargeableBlockingComponent component, DamageChangedEvent args)
    {
        if (_battery.GetBattery(uid) is not {} battery
            || !_itemToggle.IsActivated(uid)
            || args.DamageDelta == null)
            return;

        var batteryUse = args.DamageDelta.GetTotal().Float();
        _battery.TryUseCharge(battery.AsNullable(), batteryUse);
    }

    private void AttemptToggle(EntityUid uid, RechargeableBlockingComponent component, ref ItemToggleActivateAttemptEvent args)
    {
        if (!component.Discharged)
            return;

        if (HasComp<BatterySelfRechargerComponent>(uid))
            args.Popup = Loc.GetString("rechargeable-blocking-remaining-time-popup", ("remainingTime", GetRemainingTime(uid)));
        else
            args.Popup = Loc.GetString("rechargeable-blocking-not-enough-charge-popup");

        args.Cancelled = true;
    }
    private void OnChargeChanged(EntityUid uid, RechargeableBlockingComponent component, ChargeChangedEvent args)
    {
        CheckCharge(uid, component);
    }

    private void OnPowerCellChanged(EntityUid uid, RechargeableBlockingComponent component, PowerCellChangedEvent args)
    {
        CheckCharge(uid, component);
    }

    private void CheckCharge(EntityUid uid, RechargeableBlockingComponent component)
    {
        if (_battery.GetBattery(uid) is not {} battery)
            return;

        BatterySelfRechargerComponent? recharger;
        var charge = _battery.GetCharge(battery.AsNullable());
        if (charge < 1)
        {
            if (TryComp(uid, out recharger))
            {
                recharger.AutoRechargeRate = component.DischargedRechargeRate;
                Dirty(uid, recharger);
            }

            component.Discharged = true;
            _itemToggle.TryDeactivate(uid, predicted: false);
            return;
        }

        if (MathF.Round(charge / battery.Comp.MaxCharge, 2) < component.RechargePercentage)
            return;

        component.Discharged = false;
        if (TryComp(uid, out recharger))
        {
            recharger.AutoRechargeRate = component.ChargedRechargeRate;
            Dirty(uid, recharger);
        }
    }
}
