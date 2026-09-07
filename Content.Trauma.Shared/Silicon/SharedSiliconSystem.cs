// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Wraith;
using Content.Shared.Alert;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Suicide;
using Content.Trauma.Common.Body;
using Content.Trauma.Shared.Silicon.Components;

namespace Content.Trauma.Shared.Silicon;

public sealed partial class SharedSiliconChargeSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private static readonly ProtoId<DamageTypePrototype> Ion = "Ion";
    private static readonly ProtoId<DamageTypePrototype> Shock = "Shock";

    [SubscribeLocalEvent]
    private void OnSiliconInit(EntityUid uid, SiliconComponent component, ComponentInit args)
    {
        if (!component.BatteryPowered)
            return;

        _alerts.ShowAlert(uid, component.BatteryAlert, component.ChargeState);
    }

    [SubscribeLocalEvent]
    private void OnSiliconChargeStateUpdate(EntityUid uid, SiliconComponent component, SiliconChargeStateUpdateEvent ev)
    {
        _alerts.ShowAlert(uid, component.BatteryAlert, ev.ChargePercent);
    }

    [SubscribeLocalEvent]
    private void OnRefreshMovespeed(EntityUid uid, SiliconComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (!component.BatteryPowered)
            return;

        var closest = 0;

        foreach (var state in component.SpeedModifierThresholds)
        {
            if (component.ChargeState >= state.Key && state.Key > closest)
                closest = state.Key;
        }

        var speedMod = component.SpeedModifierThresholds[closest];

        args.ModifySpeed(speedMod, speedMod);
    }

    [SubscribeLocalEvent]
    private void OnTryingToSleep(Entity<SiliconComponent> ent, ref TryingToSleepEvent args)
    {
        // no rest for the wicked
        args.Cancelled = true;
    }

    // do not question why suicide splits asphyx/slash
    [SubscribeLocalEvent]
    private void OnSuicideDamage(Entity<SiliconComponent> ent, ref SuicideDamageEvent args)
    {
        args.DamageType = Ion;
    }

    [SubscribeLocalEvent(before: [typeof(SharedSuicideSystem)])]
    private void OnSuicide(Entity<SiliconComponent> ent, ref SuicideEvent args)
    {
        args.DamageType = Shock;
    }

    [SubscribeLocalEvent]
    private void OnCurseAttempt(Entity<SiliconComponent> ent, ref CurseAttemptEvent args)
    {
        _popup.PopupEntity(Loc.GetString("curse-fail-robot"), args.Curser, args.Curser);
        args.Cancelled = true;
    }
}

/// <summary>
///     Event raised when a Silicon's charge state needs to be updated.
/// </summary>
[ByRefEvent]
public record struct SiliconChargeStateUpdateEvent(short ChargePercent);
