// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Silicon.Components;
using Content.Trauma.Shared.Silicon.Systems;
using Content.Goobstation.Common.CCVar;
using Content.Shared.Alert;
using Content.Shared.Atmos.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Temperature.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Silicon.Charge;

public sealed partial class SiliconChargeSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private MovementSpeedModifierSystem _moveMod = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private EntityQuery<MindContainerComponent> _mindConQuery = default!;

    private TimeSpan _npcUpdateDelay;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, GoobCVars.SiliconNpcUpdateTime, x => _npcUpdateDelay = TimeSpan.FromSeconds(x), true);
    }

    // TODO: kill this slop, just have ipcs set power cell draw while alive with a mind
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<SiliconComponent>();
        foreach (var ent in query)
        {
            if (!ent.Comp.BatteryPowered || _mob.IsDead(ent.Owner))
                continue;

            // Check if the Silicon is an NPC, and if so, follow the delay as specified in the CVAR.
            if (ent.Comp.EntityType == SiliconType.Npc)
            {
                var remaining = now - ent.Comp.LastDrainTime;
                if (remaining < _npcUpdateDelay)
                    continue;

                ent.Comp.LastDrainTime = now;
            }

            // If you can't find a battery, set the indicator and skip it.
            if (!_powerCell.TryGetBatteryFromEntityOrSlot(ent.Owner, out var battery))
            {
                UpdateChargeState(ent, 0);
                if (_alerts.IsShowingAlert(ent.Owner, ent.Comp.BatteryAlert))
                {
                    _alerts.ClearAlert(ent.Owner, ent.Comp.BatteryAlert);
                    _alerts.ShowAlert(ent.Owner, ent.Comp.NoBatteryAlert);
                }
                continue;
            }

            // If the silicon ghosted or is SSD while still being powered, skip it.
            if (_mindConQuery.TryComp(ent, out var mc) && !mc.HasMind)
                continue;

            // TODO: fucking kid named PowerCellDraw holy shit, this spams states
            var drainRate = ent.Comp.DrainPerSecond;

            // All multipliers will be subtracted by 1, and then added together, and then multiplied by the drain rate. This is then added to the base drain rate.
            // This is to stop exponential increases, while still allowing for less-than-one multipliers.
            var drainRateFinalAddi = 0f;

            // Ensures that the drain rate is at least 10% of normal,
            // and would allow at least 4 minutes of life with a max charge, to prevent cheese.
            var batt = battery.Value.AsNullable();
            var batteryComp = battery.Value.Comp;
            drainRate += Math.Clamp(drainRateFinalAddi, drainRate * -0.9f, batteryComp.MaxCharge / 240);

            // Drain the battery.
            _battery.TryUseCharge(batt, frameTime * drainRate);

            // Figure out the current state of the Silicon.
            var chargePercent = (short) _battery.GetRemainingUses(batt, batteryComp.MaxCharge * 0.1f);

            UpdateChargeState(ent, chargePercent);
        }
    }

    /// <summary>
    ///     Checks if anything needs to be updated, and updates it.
    /// </summary>
    public void UpdateChargeState(Entity<SiliconComponent> ent, short chargePercent)
    {
        if (chargePercent == ent.Comp.ChargeState)
            return;

        ent.Comp.ChargeState = chargePercent;

        var ev = new SiliconChargeStateUpdateEvent(chargePercent);
        RaiseLocalEvent(ent, ref ev);

        _moveMod.RefreshMovementSpeedModifiers(ent.Owner);

        // If the battery was replaced and the no battery indicator is showing, replace the indicator
        if (_alerts.IsShowingAlert(ent.Owner, ent.Comp.NoBatteryAlert) && chargePercent != 0)
        {
            _alerts.ClearAlert(ent.Owner, ent.Comp.NoBatteryAlert);
            _alerts.ShowAlert(ent.Owner, ent.Comp.BatteryAlert, chargePercent);
        }
    }
}
