// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.CombatMode;
using Content.Shared.Hands;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Power.Components;
using Content.Shared.PowerCell;
using Content.Shared.Radio;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Trauma.Shared.Silicon.Charge;

namespace Content.Trauma.Shared.Silicon.Death;

/// <summary>
///     Blocks discharged silicons from interacting with their environments
///     until they recharge.
/// </summary>
/// <remarks>
///     This is horrible.
/// </remarks>
public sealed partial class SiliconDeathSystem : EntitySystem
{
    [Dependency] private SharedCombatModeSystem _combat = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;

    [SubscribeLocalEvent]
    private void OnPickupAttempt(Entity<SiliconDownOnDeadComponent> ent, ref PickupAttemptEvent args)
    {
        if (ent.Comp.Dead)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnDropAttempt(Entity<SiliconDownOnDeadComponent> ent, ref DropAttemptEvent args)
    {
        if (ent.Comp.Dead)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnInteractionAttempt(Entity<SiliconDownOnDeadComponent> ent, ref InteractionAttemptEvent args)
    {
        // Discard all verbs on any entities that don't have a drinkable battery
        // anything that slips through the cracks should be prevented by discharged
        // silicons not having ComplexInteractionComponent
        if (ent.Comp.Dead)
            args.Cancelled |= args.Target is not { } target || !_powerCell.TryGetBatteryFromEntityOrSlot(target, out _);
    }

    [SubscribeLocalEvent]
    private void OnUnequipAttempt(Entity<SiliconDownOnDeadComponent> ent, ref IsUnequippingAttemptEvent args)
    {
        if (ent.Comp.Dead)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnSiliconChargeStateUpdate(Entity<SiliconDownOnDeadComponent> ent, ref SiliconChargeStateUpdateEvent args)
    {
        if (!_powerCell.TryGetBatteryFromEntityOrSlot(ent.Owner, out var battery))
        {
            MakeDead(ent, null);
            return;
        }

        if (args.ChargePercent == 0 && ent.Comp.Dead)
            return;

        if (args.ChargePercent == 0 && !ent.Comp.Dead)
            MakeDead(ent, battery);
        else if (args.ChargePercent != 0 && ent.Comp.Dead)
            MakeAlive(ent, battery);
    }

    [SubscribeLocalEvent]
    private void OnRadioSendAttempt(Entity<SiliconDownOnDeadComponent> ent, ref RadioSendAttemptEvent args)
    {
        // Prevent talking on radio if depowered
        args.Cancelled |= ent.Comp.Dead;
    }

    /// <summary>
    ///     Some actions, like picking up an IPC and carrying it remove the KnockedDownComponent, if they try to stand when they
    ///     shouldn't, just knock them down again
    /// </summary>
    [SubscribeLocalEvent]
    private void OnStandAttempt(Entity<SiliconDownOnDeadComponent> ent, ref StandAttemptEvent args)
    {
        // Prevent standing up if discharged
        if (ent.Comp.Dead)
            args.Cancel();
    }

    private void MakeDead(Entity<SiliconDownOnDeadComponent> ent, Entity<BatteryComponent>? battery)
    {
        if (ent.Comp.Dead)
            return;

        // Disable combat mode
        if (TryComp<CombatModeComponent>(ent, out var combatMode))
        {
            _combat.SetInCombatMode(ent, false);
            _actions.SetEnabled(combatMode.CombatToggleActionEntity, false);
        }

        // Knock down
        _standing.Down(ent);
        EnsureComp<KnockedDownComponent>(ent);

        /* TODO NUBODY: reimplement this slop in the future if there's an api made
        if (TryComp(ent, out HumanoidProfileComponent? humanoid)
        {
            var layers = HumanoidVisualLayersExtension.Sublayers(HumanoidVisualLayers.HeadSide);
            _humanoid.SetLayersVisibility((ent, humanoid), layers, false);
        }
        */

        ent.Comp.Dead = true;
        ent.Comp.CanUseComplexInteractions = HasComp<ComplexInteractionComponent>(ent);
        Dirty(ent, ent.Comp);

        // Remove ComplexInteractionComponent
        RemComp<ComplexInteractionComponent>(ent);

        var ev = new SiliconChargeDeathEvent(ent, battery);
        RaiseLocalEvent(ent, ref ev);
    }

    private void MakeAlive(Entity<SiliconDownOnDeadComponent> ent, Entity<BatteryComponent>? battery)
    {
        if (!ent.Comp.Dead)
            return;

        // Enable combat mode
        if (TryComp<CombatModeComponent>(ent, out var combatMode))
            _actions.SetEnabled(combatMode.CombatToggleActionEntity, true);

        // Let you stand again
        RemComp<KnockedDownComponent>(ent);

        // Update component
        ent.Comp.Dead = false;
        Dirty(ent, ent.Comp);

        // Restore ComplexInteractionComponent
        if (ent.Comp.CanUseComplexInteractions)
            EnsureComp<ComplexInteractionComponent>(ent);

        var ev = new SiliconChargeAliveEvent(ent, battery);
        RaiseLocalEvent(ent, ref ev);
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
