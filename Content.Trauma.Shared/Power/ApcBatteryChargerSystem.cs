// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;

namespace Content.Trauma.Shared.Power;

public abstract partial class ApcBatteryChargerSystem : EntitySystem
{
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private EntityQuery<BatteryComponent> _batteryQuery = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<ApcBatteryChargerComponent> ent, ref MapInitEvent args)
    {
        UpdateDrawRate(ent, !_battery.IsFull(ent.Owner));
    }

    [SubscribeLocalEvent]
    private void OnRefreshChargeRate(Entity<ApcBatteryChargerComponent> ent, ref RefreshChargeRateEvent args)
    {
        args.NewChargeRate += ent.Comp.ChargeRate;
    }

    [SubscribeLocalEvent]
    private void OnBatteryStateChanged(Entity<ApcBatteryChargerComponent> ent, ref BatteryStateChangedEvent args)
    {
        UpdateDrawRate(ent, args.NewState != BatteryState.Full);
    }

    /// <summary>
    /// Update how much APC power the entity should draw if it's charging or not.
    /// </summary>
    public void UpdateDrawRate(Entity<ApcBatteryChargerComponent> ent, bool charging)
    {
        var drawRate = ent.Comp.IdleLoad;
        if (charging)
            drawRate += ent.Comp.DrawRate;

        _power.SetLoad(ent.Owner, drawRate);
        // assume it's either full or out of power and let power ramping correct it next tick
        // if it's actually out of power and this immediately calculated the real charge rate,
        // it might cause it to charge while not receiving any power
        // this only happens at 0/100% charge changes anyway so a tick delay doesnt matter for UX
        SetChargeRate(ent, 0f);
    }

    /// <summary>
    /// Calculate a battery's charge rate for a given received APC power.
    /// </summary>
    public float CalcChargeRate(Entity<ApcBatteryChargerComponent> ent, float power)
        => Math.Clamp((power - ent.Comp.IdleLoad) * ent.Comp.Efficiency,
            0f,
            Math.Max(1f, GetRemainingCharge(ent)));

    /// <summary>
    /// Update the battery's charge rate directly.
    /// </summary>
    public void SetChargeRate(Entity<ApcBatteryChargerComponent> ent, float rate)
    {
        if (rate == ent.Comp.ChargeRate)
            return;

        ent.Comp.ChargeRate = rate;
        Dirty(ent);
        _battery.RefreshChargeRate(ent.Owner);
    }

    private float GetRemainingCharge(EntityUid uid)
        => _batteryQuery.TryComp(uid, out var battery)
            ? battery.MaxCharge - _battery.GetCharge((uid, battery))
            : 0f;
}
