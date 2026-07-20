// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Lavaland.Common.Weapons.Ranged;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Content.Shared.Timing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Content.Shared.Wieldable.Components;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.Side;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Teleportation;
using Content.Trauma.Shared.Wizard.Projectiles;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;

namespace Content.Trauma.Shared.Heretic.Systems.Side;

public sealed partial class LionhunterRifleSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedHereticSystem _heretic = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedMansusGraspSystem _grasp = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private TeleportSystem _teleport = default!;
    [Dependency] private SharedCombatModeSystem _combat = default!;
    [Dependency] private UseDelaySystem _delay = default!;

    [Dependency] private EntityQuery<WieldableComponent> _wieldableQuery = default!;
    [Dependency] private EntityQuery<LionhunterRifleProjectileComponent> _lionhunterProjectileQuery = default!;
    [Dependency] private EntityQuery<ProjectileComponent> _projectileQuery = default!;
    [Dependency] private EntityQuery<TargetedProjectileComponent> _targetedQuery = default!;
    [Dependency] private EntityQuery<HomingProjectileComponent> _homingQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AimedRifleComponent, DoAfterAttemptEvent<AimedRifleDoAfterEvent>>(OnDoAfterAttempt);
        SubscribeLocalEvent<AimedRifleComponent, AimedRifleDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<LionhunterRifleComponent, ProjectileShotEvent>(OnShoot);
        SubscribeLocalEvent<LionhunterRifleComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<LionhunterRifleComponent, AimedRifleAimAttemptEvent>(OnAimAttempt);

        SubscribeLocalEvent<LionhunterRifleProjectileComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<LionhunterRifleProjectileComponent, ProjectileHitEvent>(OnHit);

        CommandBinds.Builder
            .Bind(EngineKeyFunctions.UseSecondary, new PointerInputCmdHandler(Aim))
            .Register<LionhunterRifleSystem>();
    }

    private bool Aim(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        if (session?.AttachedEntity is not { Valid: true } player || !Exists(player) || !Exists(uid))
            return false;

        AimRifle(player, uid);
        return false;
    }

    private void OnHit(Entity<LionhunterRifleProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        if (ent.Comp.EmpowerTarget is not { } target || args.Target != target)
            return;

        _stun.KnockdownOrStun(target, ent.Comp.KnockdownTime);

        if (ent.Comp.ShooterPath is { } path)
            _grasp.ApplyMark(target, path, ent.Comp.ShooterPassiveLevel);

        if (!_projectileQuery.TryComp(ent, out var projectile) || projectile.Shooter is not { } shooter)
            return;

        _teleport.TeleportSingle(shooter, Transform(target).Coordinates, shooter);
    }

    private void OnPreventCollide(Entity<LionhunterRifleProjectileComponent> ent, ref PreventCollideEvent args)
    {
        if (ent.Comp.EmpowerTarget is { } target && args.OtherEntity != target)
            args.Cancelled = true;
    }

    private void OnAimAttempt(Entity<LionhunterRifleComponent> ent, ref AimedRifleAimAttemptEvent args)
    {
        if (args.Cancelled || _heretic.IsHereticOrGhoul(args.User))
            return;

        args.Cancelled = true;
    }

    private void OnExamine(Entity<LionhunterRifleComponent> ent, ref ExaminedEvent args)
    {
        if (!_heretic.IsHereticOrGhoul(args.Examiner))
            return;

        args.PushMarkup(Loc.GetString("lionhunter-rifle-examine-message"));
    }

    private void OnShoot(Entity<LionhunterRifleComponent> ent, ref ProjectileShotEvent args)
    {
        if (CompOrNull<AimedRifleComponent>(ent.Owner)?.AimingAt == null || args.User is not { } user)
            return;

        HereticPath? path = null;
        var passiveLevel = 1;
        if (_heretic.TryGetHereticComponent(user, out var heretic, out _))
        {
            passiveLevel = heretic.PassiveLevel;
            path = heretic.CurrentPath;
        }

        var uid = args.FiredProjectile;

        if (!_lionhunterProjectileQuery.TryComp(uid, out var comp) ||
            !_projectileQuery.TryComp(uid, out var projectile))
            return;

        projectile.Damage = new DamageSpecifier
        {
            DamageDict =
                projectile.Damage.DamageDict.ToDictionary(x => x.Key, x => x.Value * comp.EmpowerDamageMultiplier),
            ArmorPenetration = projectile.Damage.ArmorPenetration,
            WoundSeverityMultipliers = projectile.Damage.WoundSeverityMultipliers,
        };

        Dirty(uid, projectile);

        EntityManager.AddComponents(uid, comp.ComponentsOnEmpower);

        if (!_targetedQuery.TryComp(uid, out var targeted) || targeted.Target is not { } netTarget)
            return;

        var target = GetEntity(netTarget);

        comp.ShooterPath = path;
        comp.ShooterPassiveLevel = passiveLevel;
        comp.EmpowerTarget = target;
        Dirty(uid, comp);

        if (!_homingQuery.TryComp(uid, out var homing))
            return;

        homing.Target = target;
        Dirty(uid, homing);
    }

    private void OnDoAfter(Entity<AimedRifleComponent> ent, ref AimedRifleDoAfterEvent args)
    {
        if (ent.Comp.AimingAt is not { } target)
            return;

        if (args is { Cancelled: false, Handled: false } && Exists(args.Target) && args.Target.Value == target)
        {
            args.Handled = true;

            if (!TryComp(ent, out GunComponent? gun))
                return;

            _gun.AttemptShoot(args.User, (ent, gun), Transform(target).Coordinates, target);
            _delay.TryResetDelay(ent.Owner, id: ent.Comp.AimUseDelayId);
        }

        if (ent.Comp.ShowMark)
            RemCompDeferred<AimedRifleMarkerComponent>(target);
        ent.Comp.AimingAt = null;
        Dirty(ent);
    }

    private void OnDoAfterAttempt(Entity<AimedRifleComponent> ent,
        ref DoAfterAttemptEvent<AimedRifleDoAfterEvent> args)
    {
        if (ent.Comp.AimingAt != args.Event.Target ||
            _wieldableQuery.TryComp(ent, out var wieldable) && !wieldable.Wielded ||
            args.Event.Target is not { } target || !_transform.InRange(args.Event.User, target, ent.Comp.MaxDistance))
            args.Cancel();
    }

    private void AimRifle(EntityUid user, EntityUid target)
    {
        if (target == user || !_combat.IsInCombatMode(user) || !TryComp(user, out DoAfterComponent? doAfter))
            return;

        if (!_hands.TryGetActiveItem(user, out var ent) || !TryComp(ent.Value, out AimedRifleComponent? comp) ||
            _delay.IsDelayed(ent.Value, comp.AimUseDelayId))
            return;

        if (_whitelist.IsWhitelistFail(comp.AimWhitelist, target))
            return;

        var ev = new AimedRifleAimAttemptEvent(ent.Value, user, target);
        RaiseLocalEvent(ent.Value, ref ev);
        if (ev.Cancelled)
            return;

        if (_wieldableQuery.TryComp(ent.Value, out var wieldable) && !wieldable.Wielded)
        {
            _popup.PopupEntity(Loc.GetString("wieldable-component-requires", ("item", ent.Value)), user, user);
            return;
        }

        var coords = Transform(user).Coordinates;
        var otherCoords = Transform(target).Coordinates;
        if (!coords.TryDistance(EntityManager, _transform, otherCoords, out var distance) ||
            distance > comp.MaxDistance)
            return;

        if (distance < comp.MinDistance)
        {
            _popup.PopupEntity(Loc.GetString("heretic-ability-fail-too-close"), user, user);
            return;
        }

        var time = comp.AimTimePerDistance * distance;
        if (time > comp.MaxAimTime)
            time = comp.MaxAimTime;

        var doArgs = new DoAfterArgs(EntityManager,
            user,
            time,
            new AimedRifleDoAfterEvent(),
            ent.Value,
            target,
            ent.Value)
        {
            MultiplyDelay = false,
            AttemptFrequency = AttemptFrequency.EveryTick,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
            NeedHand = true,
            RequireCanInteract = false,
            DistanceThreshold = null,
            DuplicateCondition = DuplicateConditions.SameTarget,
        };

        var dirtied = false;

        // If we are already aiming at target, do-after will be cancelled because of DuplicateConditions
        if (comp.AimingAt != target)
        {
            if (comp.ShowMark && Exists(comp.AimingAt))
                RemCompDeferred<AimedRifleMarkerComponent>(comp.AimingAt.Value);

            // Cancel all other aiming do-afters, we can't aim at multiple targets at once
            // Not relying on DuplicateConditions because we need new do-after to be prioritized over old ones
            foreach (var (id, da) in doAfter.DoAfters)
            {
                if (da.Cancelled || da.Completed)
                    continue;

                if (_doAfter.GetArgs(da).Event.GetType() != typeof(AimedRifleDoAfterEvent))
                    continue;

                _doAfter.Cancel(user, id, doAfter, true);
            }

            comp.AimingAt = target;
            Dirty(ent.Value, comp);
            dirtied = true;

        }

        if (_doAfter.TryStartDoAfter(doArgs))
        {
            _popup.PopupEntity(Loc.GetString("lionhunter-rifle-aim-message"), user, user);
            if (comp.ShowMark)
                EnsureComp<AimedRifleMarkerComponent>(target);
            return;
        }

        comp.AimingAt = null;
        if (!dirtied)
            Dirty(ent.Value, comp);
    }

    [Serializable, NetSerializable]
    private sealed partial class AimedRifleDoAfterEvent : SimpleDoAfterEvent;
}
