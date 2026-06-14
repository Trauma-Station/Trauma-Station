// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Radio;
using Content.Shared.Actions;
using Content.Shared.CombatMode;
using Content.Shared.Interaction.Components;
using Content.Shared.Power.Components;
using Content.Shared.PowerCell;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Trauma.Shared.Silicon.Death;
using Content.Trauma.Shared.Silicon.Systems;

namespace Content.Trauma.Server.Silicon.Death;

public sealed partial class SiliconDeathSystem : SharedSiliconDeathSystem
{
    [Dependency] private SharedCombatModeSystem _combat = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconDownOnDeadComponent, SiliconChargeStateUpdateEvent>(OnSiliconChargeStateUpdate);

        SubscribeLocalEvent<SiliconDownOnDeadComponent, RadioSendAttemptEvent>(OnRadioSendAttempt);
        SubscribeLocalEvent<SiliconDownOnDeadComponent, StandAttemptEvent>(OnStandAttempt);
    }

    private void OnSiliconChargeStateUpdate(EntityUid uid, SiliconDownOnDeadComponent siliconDeadComp, SiliconChargeStateUpdateEvent args)
    {
        if (!_powerCell.TryGetBatteryFromEntityOrSlot(uid, out var battery))
        {
            SiliconDead(uid, siliconDeadComp, battery);
            return;
        }

        if (args.ChargePercent == 0 && siliconDeadComp.Dead)
            return;

        if (args.ChargePercent == 0 && !siliconDeadComp.Dead)
            SiliconDead(uid, siliconDeadComp, battery);
        else if (args.ChargePercent != 0 && siliconDeadComp.Dead)
            SiliconUnDead(uid, siliconDeadComp, battery);
    }

    // Goobstation - Energycrit
    private void OnRadioSendAttempt(Entity<SiliconDownOnDeadComponent> ent, ref RadioSendAttemptEvent args)
    {
        // Prevent talking on radio if depowered
        if (args.Cancelled || !ent.Comp.Dead)
            return;

        args.Cancelled = true;
    }

    // Goobstation - Energycrit
    /// <summary>
    ///     Some actions, like picking up an IPC and carrying it remove the KnockedDownComponent, if they try to stand when they
    ///     shouldn't, just knock them down again
    /// </summary>
    private void OnStandAttempt(Entity<SiliconDownOnDeadComponent> ent, ref StandAttemptEvent args)
    {
        // Prevent standing up if discharged
        if (ent.Comp.Dead)
            args.Cancel();
    }

    private void SiliconDead(EntityUid uid, SiliconDownOnDeadComponent siliconDeadComp, Entity<BatteryComponent>? battery)
    {
        if (siliconDeadComp.Dead)
            return;

        // Disable combat mode
        if (TryComp<CombatModeComponent>(uid, out var combatMode))
        {
            _combat.SetInCombatMode(uid, false);
            _actions.SetEnabled(combatMode.CombatToggleActionEntity, false);
        }

        // Knock down
        _standing.Down(uid);
        EnsureComp<KnockedDownComponent>(uid);

        /* TODO NUBODY: reimplement this slop in the future if there's an api made
        if (TryComp(uid, out HumanoidProfileComponent? humanoid)
        {
            var layers = HumanoidVisualLayersExtension.Sublayers(HumanoidVisualLayers.HeadSide);
            _humanoid.SetLayersVisibility((uid, humanoid), layers, false);
        }
        */

        // SiliconDownOnDeadComponent moved to shared
        siliconDeadComp.Dead = true;
        siliconDeadComp.CanUseComplexInteractions = HasComp<ComplexInteractionComponent>(uid);
        Dirty(uid, siliconDeadComp);

        // Remove ComplexInteractionComponent
        RemComp<ComplexInteractionComponent>(uid);

        var ev = new SiliconChargeDeathEvent(uid, battery);
        RaiseLocalEvent(uid, ref ev);
    }

    private void SiliconUnDead(EntityUid uid, SiliconDownOnDeadComponent siliconDeadComp, Entity<BatteryComponent>? battery)
    {
        if (!siliconDeadComp.Dead)
            return;

        // Enable combat mode
        if (TryComp<CombatModeComponent>(uid, out var combatMode))
            _actions.SetEnabled(combatMode.CombatToggleActionEntity, true);

        // Let you stand again
        RemComp<KnockedDownComponent>(uid);

        // Update component
        siliconDeadComp.Dead = false;
        Dirty(uid, siliconDeadComp);

        // Restore ComplexInteractionComponent
        if (siliconDeadComp.CanUseComplexInteractions)
            EnsureComp<ComplexInteractionComponent>(uid);

        var ev = new SiliconChargeAliveEvent(uid, battery);
        RaiseLocalEvent(uid, ref ev);
    }
}

/// <summary>
///     An event raised after a Silicon has gone down due to charge.
/// </summary>
[ByRefEvent]
public readonly record struct SiliconChargeDeathEvent(EntityUid Silicon, Entity<BatteryComponent>? Battery);

/// <summary>
///     An event raised after a Silicon has reawoken due to an increase in charge.
/// </summary>
[ByRefEvent]
public readonly record struct SiliconChargeAliveEvent(EntityUid Silicon, Entity<BatteryComponent>? Battery);
