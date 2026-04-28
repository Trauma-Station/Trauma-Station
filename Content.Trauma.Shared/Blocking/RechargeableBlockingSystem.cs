// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Systems;
using Content.Shared.Emp;
using Content.Shared.Examine;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Trauma.Common.Blocking;

namespace Content.Trauma.Shared.Blocking;

// TODO: move this to shared goobmod and predict
public sealed class RechargeableBlockingSystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RechargeableBlockingComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RechargeableBlockingComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<RechargeableBlockingComponent, ItemToggleActivateAttemptEvent>(AttemptToggle);
        SubscribeLocalEvent<RechargeableBlockingComponent, ChargeChangedEvent>(CheckCharge);
        SubscribeLocalEvent<RechargeableBlockingComponent, PowerCellChangedEvent>(CheckCharge);
        SubscribeLocalEvent<RechargeableBlockingComponent, EmpPulseEvent>(OnEmpPulse,
            after: [typeof(SharedBatterySystem)]); // need to override its value
    }

    private void OnExamined(Entity<RechargeableBlockingComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.Discharged)
            return;

        args.PushMarkup(Loc.GetString("rechargeable-blocking-discharged"));
        args.PushMarkup(Loc.GetString("rechargeable-blocking-remaining-time", ("remainingTime", GetRemainingTime(ent))));
    }

    private int GetRemainingTime(EntityUid uid)
    {
        if (_battery.GetBattery(uid) is not { } battery || battery.Comp.ChargeRate == 0)
            return 0;

        var remaining = battery.Comp.MaxCharge - _battery.GetCharge(battery.AsNullable());
        return (int) MathF.Round(remaining / battery.Comp.ChargeRate);
    }

    private void OnDamageChanged(Entity<RechargeableBlockingComponent> ent, ref DamageChangedEvent args)
    {
        if (_battery.GetBattery(ent.Owner) is not { } battery ||
            !_toggle.IsActivated(ent.Owner) ||
            args.DamageDelta is not { } delta)
            return;

        var batteryUse = delta.GetTotal().Float();
        _battery.TryUseCharge(battery.AsNullable(), batteryUse);
    }

    private void AttemptToggle(Entity<RechargeableBlockingComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        CheckCharge(ent, args.User);

        if (!ent.Comp.Discharged)
            return; // allow enabling it

        if (HasComp<BatterySelfRechargerComponent>(ent))
            args.Popup = Loc.GetString("rechargeable-blocking-remaining-time-popup", ("remainingTime", GetRemainingTime(ent)));
        else
            args.Popup = Loc.GetString("rechargeable-blocking-not-enough-charge-popup");

        args.Cancelled = true;
    }

    private void CheckCharge<T>(Entity<RechargeableBlockingComponent> ent, ref T args) where T : notnull
    {
        CheckCharge(ent);
    }

    private void CheckCharge(Entity<RechargeableBlockingComponent> ent, EntityUid? user = null)
    {
        if (_battery.GetBattery(ent.Owner) is not { } battery)
        {
            SetDischarged(ent, user);
            return;
        }

        var charge = _battery.GetCharge(battery.AsNullable());
        if (charge < 1)
        {
            SetDischarged(ent, user);
            return;
        }

        if (MathF.Round(charge / battery.Comp.MaxCharge, 2) < ent.Comp.RechargePercentage)
            return;

        SetDischarged(ent, user, false);
    }

    private void OnEmpPulse(Entity<RechargeableBlockingComponent> ent, ref EmpPulseEvent args)
    {
        // don't disable the battery via EMP, we have our own logic
        args.Disabled = false;
    }

    private void SetDischarged(Entity<RechargeableBlockingComponent> ent, EntityUid? user = null, bool discharged = true)
    {
        if (ent.Comp.Discharged == discharged)
            return;

        ent.Comp.Discharged = discharged;
        Dirty(ent);

        if (discharged)
            _toggle.TryDeactivate(ent.Owner, user: user, predicted: true);

        if (TryComp<BatterySelfRechargerComponent>(ent, out var recharger))
        {
            recharger.AutoRechargeRate = discharged
                ? ent.Comp.DischargedRechargeRate
                : ent.Comp.ChargedRechargeRate;
            Dirty(ent, recharger);
        }
    }
}
