// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Silicon.DeadStartupButton;
using Content.Server.Lightning;
using Content.Server.Lightning.Components;
using Content.Server.Popups;
using Content.Shared.Audio;
using Content.Shared.Electrocution;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Robust.Shared.Random;

namespace Content.Trauma.Server.Silicon.DeadStartupButton;

public sealed partial class DeadStartupButtonSystem : SharedDeadStartupButtonSystem
{
    [Dependency] private MobThresholdSystem _threshold = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private LightningSystem _lightning = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private SharedBatterySystem _battery = default!;

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
            || !Mob.IsDead(uid, mobStateComponent)
            || !TryComp<MobThresholdsComponent>(uid, out var mobThresholdsComponent))
            return;

        var damage = _threshold.CheckVitalDamage(uid);
        // Check if entity has a critical state
        if (_threshold.TryGetThresholdForState(uid, MobState.Critical, out var criticalThreshold, mobThresholdsComponent)
            && damage < criticalThreshold)
        {
            Mob.ChangeMobState(uid, MobState.Alive, mobStateComponent);
            return;
        }

        // Check if entity has a dead state
        if (_threshold.TryGetThresholdForState(uid, MobState.Dead, out var deadThreshold, mobThresholdsComponent)
            && damage < deadThreshold)
        {
            Mob.ChangeMobState(uid, MobState.Alive, mobStateComponent);
            return;
        }

        Audio.PlayPvs(comp.BuzzSound, uid);
        var name = Identity.Entity(uid, EntityManager);
        _popup.PopupEntity(Loc.GetString("dead-startup-system-reboot-failed", ("target", name)), uid);
        Spawn("EffectSparks", Transform(uid).Coordinates);
    }

    private void OnElectrocuted(EntityUid uid, DeadStartupButtonComponent comp, ElectrocutedEvent args)
    {
        if (HasComp<LightningComponent>(args.SourceUid)
            || !TryComp<MobStateComponent>(uid, out var mobStateComponent)
            || !Mob.IsDead(uid, mobStateComponent)
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

        var name = Identity.Entity(uid, EntityManager);
        _popup.PopupEntity(Loc.GetString("dead-startup-system-reboot-success", ("target", name)), uid);
        Audio.PlayPvs(comp.Sound, uid);
    }
}
