// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._EinsteinEngines.Silicon.Systems;
using Content.Shared.Bed.Sleep;
using Content.Server._EinsteinEngines.Silicon.Charge;
using Content.Server.Humanoid;
using Content.Shared.Humanoid;
using Content.Shared.Power.Components;
using Content.Shared.StatusEffectNew;
// Goobstation Start - Energycrit
using Content.Goobstation.Shared.Sprinting;
using Content.Server.Radio;
using Content.Shared._EinsteinEngines.Silicon.Death;
using Content.Shared.Actions;
using Content.Shared.CombatMode;
using Content.Shared.Interaction.Components;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
// Goobstation End - Energycrit

namespace Content.Server._EinsteinEngines.Silicon.Death;

public sealed class SiliconDeathSystem : SharedSiliconDeathSystem
{
    [Dependency] private readonly SleepingSystem _sleep = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearanceSystem = default!;
    // Goobstation Start - Energycrit
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    // Goobstation End - Energycrit

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
        if (args.Cancelled || !ent.Comp.Dead)
            return;

        EnsureComp<KnockedDownComponent>(ent);
        args.Cancel();
    }

    private void SiliconDead(EntityUid uid, SiliconDownOnDeadComponent siliconDeadComp, Entity<PredictedBatteryComponent>? battery)
    {
        if (siliconDeadComp.Dead)
            return;

        // Disable sprinting.
        if (TryComp<SprinterComponent>(uid, out var sprint))
        {
            sprint.CanSprint = false;
            Dirty(uid, sprint);
        }

        // Disable combat mode
        if (TryComp<CombatModeComponent>(uid, out var combatMode))
        {
            _combat.SetInCombatMode(uid, false);
            _actions.SetEnabled(combatMode.CombatToggleActionEntity, false);
        }

        // Knock down
        _standing.Down(uid);
        EnsureComp<KnockedDownComponent>(uid);

        if (TryComp(uid, out HumanoidAppearanceComponent? humanoidAppearanceComponent))
        {
            var layers = HumanoidVisualLayersExtension.Sublayers(HumanoidVisualLayers.HeadSide);
            _humanoidAppearanceSystem.SetLayersVisibility((uid, humanoidAppearanceComponent), layers, false);
        }

        // SiliconDownOnDeadComponent moved to shared
        siliconDeadComp.Dead = true;
        siliconDeadComp.CanUseComplexInteractions = HasComp<ComplexInteractionComponent>(uid);
        Dirty(uid, siliconDeadComp);

        // Remove ComplexInteractionComponent
        RemComp<ComplexInteractionComponent>(uid);

        var ev = new SiliconChargeDeathEvent(uid, battery);
        RaiseLocalEvent(uid, ref ev);
    }

    private void SiliconUnDead(EntityUid uid, SiliconDownOnDeadComponent siliconDeadComp, Entity<PredictedBatteryComponent>? battery)
    {
        if (!siliconDeadComp.Dead)
            return;

        // Enable sprinting
        if (TryComp<SprinterComponent>(uid, out var sprint))
        {
            sprint.CanSprint = true;
            Dirty(uid, sprint);
        }

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
public readonly record struct SiliconChargeDeathEvent(EntityUid Silicon, Entity<PredictedBatteryComponent>? Battery);

/// <summary>
///     An event raised after a Silicon has reawoken due to an increase in charge.
/// </summary>
[ByRefEvent]
public readonly record struct SiliconChargeAliveEvent(EntityUid Silicon, Entity<PredictedBatteryComponent>? Battery);
