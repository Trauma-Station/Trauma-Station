// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Silicon.DeadStartupButton;
using Content.Server.Lightning;
using Content.Server.Lightning.Components;
using Content.Server.Popups;
using Content.Shared.Audio;
using Content.Shared.Electrocution;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Trauma.Server.Silicon.DeadStartupButton;

public sealed class DeadStartupButtonSystem : SharedDeadStartupButtonSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeadStartupButtonComponent, DeadStartupDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<DeadStartupButtonComponent, ElectrocutedEvent>(OnElectrocuted);
        SubscribeLocalEvent<DeadStartupButtonComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnDoAfter(EntityUid uid, DeadStartupButtonComponent comp, DeadStartupDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled
            || !TryComp<MobStateComponent>(uid, out var mobStateComponent)
            || !_mobState.IsDead(uid, mobStateComponent)
            || !TryComp<MobThresholdsComponent>(uid, out var mobThresholdsComponent))
            return;

        var damage = _mobThreshold.CheckVitalDamage(uid);
        // Check if entity has a critical state
        if (_mobThreshold.TryGetThresholdForState(uid, MobState.Critical, out var criticalThreshold, mobThresholdsComponent)
            && damage < criticalThreshold)
        {
            _mobState.ChangeMobState(uid, MobState.Alive, mobStateComponent);
            return;
        }

        // Check if entity has a dead state
        if (_mobThreshold.TryGetThresholdForState(uid, MobState.Dead, out var deadThreshold, mobThresholdsComponent)
            && damage < deadThreshold)
        {
            _mobState.ChangeMobState(uid, MobState.Alive, mobStateComponent);
            return;
        }

        _audio.PlayPvs(comp.BuzzSound, uid, AudioHelpers.WithVariation(0.05f, _robustRandom));
        _popup.PopupEntity(Loc.GetString("dead-startup-system-reboot-failed", ("target", Name(uid))), uid);
        Spawn("EffectSparks", Transform(uid).Coordinates);
    }

    private void OnElectrocuted(EntityUid uid, DeadStartupButtonComponent comp, ElectrocutedEvent args)
    {
        if (HasComp<LightningComponent>(args.SourceUid) // Goobstation - Fix IPC shock loops.
            || !TryComp<MobStateComponent>(uid, out var mobStateComponent)
            || !_mobState.IsDead(uid, mobStateComponent)
            || !_powerCell.TryGetBatteryFromEntityOrSlot(uid, out var battery)
            || _battery.GetCharge(battery.Value.AsNullable()) <= 0)
            return;

        _lightning.ShootRandomLightnings(uid, 2, 4);
        _battery.SetCharge(battery.Value.AsNullable(), 0);
    }

    private void OnMobStateChanged(EntityUid uid, DeadStartupButtonComponent comp, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive)
            return;

        _popup.PopupEntity(Loc.GetString("dead-startup-system-reboot-success", ("target", MetaData(uid).EntityName)), uid);
        _audio.PlayPvs(comp.Sound, uid);
    }

}
