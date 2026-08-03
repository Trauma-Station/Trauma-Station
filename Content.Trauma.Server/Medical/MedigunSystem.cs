// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Power.EntitySystems;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Power.Components;
using Content.Shared.Timing;
using Content.Shared.Whitelist;
using Content.Trauma.Shared.Medical.Medigun;
using Content.Trauma.Shared.Medical.Medigun.Components;
using Content.Trauma.Shared.Physics.ComplexJoint;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Medical;

public sealed partial class MedigunSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedActionsSystem _action = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private AlertsSystem _alert = default!;
    [Dependency] private BatterySystem _battery = default!;
    [Dependency] private SharedBloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private ExplosionSystem _explosion = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private ItemToggleSystem _toggle = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;
    [Dependency] private SharedComplexJointVisualsSystem _joint = default!;

    [Dependency] private EntityQuery<BatteryComponent> _batteryQuery = default!;
    [Dependency] private EntityQuery<DamageableComponent> _damageableQuery = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var query = EntityQueryEnumerator<MediGunComponent>();
        while (query.MoveNext(out var medical, out var component))
        {
            if (!component.IsActive)
                continue;

            if (now < component.NextTick)
                continue;

            var medGunEnt = (medical, component);
            if (now > component.UberEndTime)
                DisableUber(medGunEnt);

            component.NextTick = now + TimeSpan.FromSeconds(component.Frequency);

            // Add uber action if we can
            if (component.UberPoints > component.PointsToUber
                && component.ParentEntity != null
                && !component.UberActivated)
                _action.AddAction(component.ParentEntity.Value, ref component.UberAction, component.UberActionId, medical);
        }
    }

    [SubscribeLocalEvent]
    private void BeamCollision(Entity<MediGunComponent> ent, ref ComplexJointCollisionEvent args)
    {
        if (args.Data.Id != ent.Comp.JointKey || ent.Comp.HealedEntities.Contains(args.Hit.HitEntity))
            return;

        DisableConnection(ent, args.Target);
    }

    [SubscribeLocalEvent]
    private void UpdateBeams(Entity<MediGunComponent> ent, ref ComplexJointUpdateEvent args)
    {
        if (!args.UpdatedIds.TryGetValue(ent.Comp.JointKey, out var set))
            return;

        foreach (var healed in ent.Comp.HealedEntities)
        {
            if (!set.Contains(healed) || !MediGunHealingTick(ent, healed))
                DisableConnection(ent, healed);
        }
    }

    /// <summary>
    /// Returns false if target had failed to be healed.
    /// </summary>
    private bool MediGunHealingTick(Entity<MediGunComponent> ent, EntityUid healed)
    {
        if (TerminatingOrDeleted(healed))
            return false;

        var comp = ent.Comp;

        var batteryToWithdraw = comp.UberActivated ? comp.UberBatteryWithdraw: comp.BatteryWithdraw;
        if (_batteryQuery.TryComp(ent.Owner, out var batteryComp)
            && !_battery.TryUseCharge((ent.Owner, batteryComp), batteryToWithdraw))
        {
            _battery.SetCharge((ent, batteryComp), 0); // Trigger recharging & cooldown
            return false;
        }

        // Do the damage (heal)
        if (!_damageableQuery.TryComp(healed, out var damageable))
            return false;

        var healing = comp.UberActivated ? comp.UberHealing : comp.Healing;
        healing *= ent.Comp.Frequency;
        var originalDamage = _damage.GetTotalDamage((healed, damageable));

        _damage.ChangeDamage(
            (healed, damageable),
            healing,
            true,
            false,
            ent.Comp.ParentEntity,
            partMultiplier: 1.0f,
            targetPart: TargetBodyPart.All,
            ignoreBlockers: true,
            splitDamage: SplitDamageBehavior.SplitEnsureAll,
            canMiss: false);

        _bloodstreamSystem.TryModifyBloodLevel(healed, comp.BleedingAmountModifier);

        var afterDamage = _damage.GetTotalDamage((healed, damageable));
        var healedAmount = originalDamage - afterDamage;

        if (!comp.UberActivated)
            comp.UberPoints += healedAmount.Float();

        if (comp.ParentEntity != null)
            UpdateAlert(comp.ParentEntity.Value, ent);

        return true;
    }

    [SubscribeLocalEvent]
    private void OnToggled(Entity<MediGunComponent> ent, ref ItemToggledEvent args)
    {
        if (ent.Comp.ParentEntity != null)
            UpdateAlert(ent.Comp.ParentEntity.Value, ent);

        // Player should pick the target by interacting with it.
        if (args.Activated)
            return;

        // Handle disabling
        DisableAllConnections(ent);
    }

    [SubscribeLocalEvent]
    private void OnActivate(Entity<MediGunComponent> ent, ref AfterInteractEvent args)
    {
        var (uid, comp) = ent;

        if (args.Target == null
            || args.Target.Value == args.User)
            return;

        if (_useDelay.IsDelayed(uid))
            return;

        if (comp.HealedEntities.Count >= comp.MaxLinksAmount)
            return;

        var target = args.Target.Value;

        if (!_whitelist.IsWhitelistPass(comp.HealAbleWhitelist, target) ||
            comp.HealedEntities.Contains(target))
            return;

        if (HasComp<MediGunHealedComponent>(target))
        {
            // boom
            _explosion.QueueExplosion(uid, "Default", 20, 3, 3.4f, 1f, 0, false, args.User);
            QueueDel(uid);
            return;
        }

        if (!_toggle.TryActivate(uid, args.User))
            return;

        _audio.PlayPvs(comp.SoundOnTarget, uid);

        // Medigun component
        comp.HealedEntities.Add(target);
        comp.IsActive = true;
        comp.ParentEntity = args.User;
        comp.NextTick = _timing.CurTime + TimeSpan.FromSeconds(comp.Frequency);
        Dirty(uid, comp);

        // Joint visuals
        var sprite = comp.UberActivated ? comp.UberBeamSprite : comp.BeamSprite;
        var color = comp.UberActivated ? comp.UberLineColor : comp.DefaultLineColor;
        var visuals = new ComplexJointVisualsData(ent.Comp.JointKey, sprite, ent.Comp.MaxRange)
        {
            Color = color,
            ReturnOnFirstHit = true,
        };
        _joint.CreateJoint(target, ent, visuals);

        // Target's component
        var mediGunned = EnsureComp<MediGunHealedComponent>(target);
        mediGunned.Source = uid;
        mediGunned.LineColor = comp.UberActivated ? comp.UberLineColor : comp.DefaultLineColor;
        Dirty(target, mediGunned);

        UpdateAlert(target, ent);
        _useDelay.TryResetDelay(uid);
        args.Handled = true;
    }

    private void UpdateAlert(EntityUid target, Entity<MediGunComponent> medigun)
    {
        var comp = medigun.Comp;
        var parent = Transform(medigun).ParentUid;

        if (parent != comp.ParentEntity ||
            !_toggle.IsActivated(medigun.Owner))
        {
            _alert.ClearAlert(target, "MedigunUberBattery");
            return;
        }

        var severity = (short) MathF.Round(comp.UberPoints / comp.PointsToUber * 10f);
        const short minSeverity = 0;
        const short maxSeverity = 10;
        severity = Math.Clamp(severity, minSeverity, maxSeverity);

        if (comp.UberActivated)
            severity = 11;

        _alert.ShowAlert(target, "MedigunUberBattery", severity);
    }

    [SubscribeLocalEvent]
    private void OnParentChanged(Entity<MediGunComponent> ent, ref EntParentChangedMessage args)
    {
        if (args.Transform.ParentUid == ent.Comp.ParentEntity)
            return;

        if (args.OldParent != null)
            UpdateAlert(args.OldParent.Value, ent);

        // Disable our gun
        DisableAllConnections(ent);
    }

    [SubscribeLocalEvent]
    private void OnUber(EntityUid uid, MediGunComponent component, MediGunUberActivateActionEvent args) =>
        EnableUber((uid, component));

    /// <summary>
    /// Activates uber mode for this medigun and changes all visuals.
    /// </summary>
    private void EnableUber(Entity<MediGunComponent> ent)
    {
        var comp = ent.Comp;

        _audio.PlayPvs(comp.SoundOnTarget, ent);
        comp.UberActivated = true;
        comp.UberEndTime = _timing.CurTime + TimeSpan.FromSeconds(comp.UberDefaultLenght);
        comp.UberPoints = 0;
        _action.RemoveAction(comp.UberAction);
        Dirty(ent);

        var visuals = EnsureComp<ComplexJointVisualsComponent>(ent);

        foreach (var (_, data) in visuals.Data)
        {
            data.Sprite = ent.Comp.UberBeamSprite;
            data.Color = ent.Comp.UberLineColor;
        }

        Dirty(ent, visuals);

        // Update beam for each target
        foreach (var healed in comp.HealedEntities)
        {
            if (!TryComp<MediGunHealedComponent>(healed, out var healComp))
                continue;

            healComp.LineColor = comp.UberLineColor;
            Dirty(healed, healComp);


        }
    }

    /// <summary>
    /// Removes all uber related values and restores normal visuals.
    /// </summary>
    private void DisableUber(Entity<MediGunComponent> ent)
    {
        var comp = ent.Comp;
        comp.UberActivated = false;
        comp.UberEndTime = TimeSpan.Zero;
        Dirty(ent);

        var visuals = EnsureComp<ComplexJointVisualsComponent>(ent);

        foreach (var (_, data) in visuals.Data)
        {
            data.Sprite = ent.Comp.BeamSprite;
            data.Color = ent.Comp.DefaultLineColor;
        }

        Dirty(ent, visuals);

        foreach (var healed in comp.HealedEntities)
        {
            if (!TryComp<MediGunHealedComponent>(healed, out var healComp))
                continue;

            healComp.LineColor = comp.DefaultLineColor;
            Dirty(healed, healComp);
        }
    }

    /// <summary>
    /// Handles removing all connections from medigun when it's disabling.
    /// Also does the full job with disabling medigun.
    /// </summary>
    private void DisableAllConnections(Entity<MediGunComponent> ent)
    {
        var comp = ent.Comp;
        foreach (var healed in comp.HealedEntities)
        {
            if (!TryComp<MediGunHealedComponent>(healed, out var mediGunned))
                return;

            RemComp(healed, mediGunned);
        }

        comp.HealedEntities.Clear();
        ClearJoints(ent);
    }

    private void ClearJoints(Entity<MediGunComponent> ent)
    {
        _toggle.TryDeactivate(ent.Owner, ent.Comp.ParentEntity);

        _joint.ClearBeamJoints(ent.Owner, ent.Comp.JointKey);

        if (ent.Comp.ParentEntity != null)
            UpdateAlert(ent.Comp.ParentEntity.Value, ent);

        ent.Comp.IsActive = false;
        ent.Comp.ParentEntity = null;
    }

    /// <summary>
    /// Disables a connection to a specific entity. Also removes it from HealedEntities list.
    /// </summary>
    private void DisableConnection(Entity<MediGunComponent> ent, EntityUid toRemove)
    {
        RemCompDeferred<MediGunHealedComponent>(toRemove);
        ent.Comp.HealedEntities.Remove(toRemove);
        Dirty(ent);

        if (ent.Comp.HealedEntities.Count == 0)
            ClearJoints(ent);
        else
            _joint.ClearBeamJoints(ent.Owner, ent.Comp.JointKey, toRemove);
    }
}
