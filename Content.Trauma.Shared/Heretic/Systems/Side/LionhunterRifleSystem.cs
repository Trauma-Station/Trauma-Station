using System.Linq;
using Content.Lavaland.Common.Weapons.Ranged;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Content.Shared.Wieldable.Components;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.Side;
using Content.Trauma.Shared.Teleportation;
using Content.Trauma.Shared.Wizard.Projectiles;
using Robust.Shared.Physics.Events;

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

    [Dependency] private EntityQuery<WieldableComponent> _wieldableQuery = default!;
    [Dependency] private EntityQuery<LionhunterRifleProjectileComponent> _lionhunterProjectileQuery = default!;
    [Dependency] private EntityQuery<ProjectileComponent> _projectileQuery = default!;
    [Dependency] private EntityQuery<TargetedProjectileComponent> _targetedQuery = default!;
    [Dependency] private EntityQuery<HomingProjectileComponent> _homingQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LionhunterRifleComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<LionhunterRifleComponent, DoAfterAttemptEvent<HereticAimGunDoAfterEvent>>(OnDoAfterAttempt);
        SubscribeLocalEvent<LionhunterRifleComponent, HereticAimGunDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<LionhunterRifleComponent, ProjectileShotEvent>(OnShoot);
        SubscribeLocalEvent<LionhunterRifleComponent, ExaminedEvent>(OnExamine);

        SubscribeLocalEvent<LionhunterRifleProjectileComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<LionhunterRifleProjectileComponent, ProjectileHitEvent>(OnHit);
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

    private void OnExamine(Entity<LionhunterRifleComponent> ent, ref ExaminedEvent args)
    {
        if (!_heretic.IsHereticOrGhoul(args.Examiner))
            return;

        args.PushMarkup(Loc.GetString("lionhunter-rifle-examine-message"));
    }

    private void OnShoot(Entity<LionhunterRifleComponent> ent, ref ProjectileShotEvent args)
    {
        if (ent.Comp.AimMarker == null || args.User is not { } user)
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

    private void OnDoAfter(Entity<LionhunterRifleComponent> ent, ref HereticAimGunDoAfterEvent args)
    {
        if (args is { Cancelled: false, Handled: false } && Exists(args.Target))
        {
            args.Handled = true;

            if (!TryComp(ent, out GunComponent? gun))
                return;

            _gun.AttemptShoot(args.User, (ent, gun), Transform(args.Target.Value).Coordinates, args.Target.Value);
        }

        PredictedQueueDel(ent.Comp.AimMarker);
        ent.Comp.AimMarker = null;
        Dirty(ent);
    }

    private void OnDoAfterAttempt(Entity<LionhunterRifleComponent> ent,
        ref DoAfterAttemptEvent<HereticAimGunDoAfterEvent> args)
    {
        if (_wieldableQuery.TryComp(ent, out var wieldable) && !wieldable.Wielded ||
            args.Event.Target is not { } target || !Transform(target)
                .Coordinates
                .TryDistance(EntityManager, _transform, Transform(args.Event.User).Coordinates, out var dist) ||
            dist > ent.Comp.MaxDistance)
            args.Cancel();
    }

    private void OnAfterInteract(Entity<LionhunterRifleComponent> ent, ref AfterInteractEvent args)
    {
        if (!_heretic.IsHereticOrGhoul(args.User))
            return;

        if (!_hands.IsHolding(args.User, ent))
            return;

        if (_wieldableQuery.TryComp(ent, out var wieldable) && !wieldable.Wielded)
        {
            _popup.PopupClient(Loc.GetString("heretic-ability-fail-not-wielded", ("ent", ent.Owner)),
                args.User,
                args.User);
            return;
        }

        if (args.Target is not { } target || target == args.User || _whitelist.IsWhitelistFail(ent.Comp.AimWhitelist, target))
            return;

        var coords = Transform(args.User).Coordinates;
        var otherCoords = Transform(target).Coordinates;
        if (!coords.TryDistance(EntityManager, _transform, otherCoords, out var distance) ||
            distance > ent.Comp.MaxDistance)
            return;

        if (distance < ent.Comp.MinDistance)
        {
            _popup.PopupClient(Loc.GetString("heretic-ability-fail-too-close"), args.User, args.User);
            return;
        }

        var time = ent.Comp.AimTimePerDistance * distance;
        if (time > ent.Comp.MaxAimTime)
            time = ent.Comp.MaxAimTime;

        var doArgs = new DoAfterArgs(EntityManager,
            args.User,
            time,
            new HereticAimGunDoAfterEvent(),
            ent,
            target,
            ent)
        {
            MultiplyDelay = false,
            AttemptFrequency = AttemptFrequency.EveryTick,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
            RequireCanInteract = false,
            DistanceThreshold = null,
        };

        if (!_doAfter.TryStartDoAfter(doArgs))
            return;

        _popup.PopupClient(Loc.GetString("lionhunter-rifle-aim-message"), args.User, args.User);

        ent.Comp.AimMarker = PredictedSpawnAttachedTo(ent.Comp.AimMarkerProto, target.ToCoordinates());
        Dirty(ent);

        args.Handled = true;
    }

    [Serializable, NetSerializable]
    private sealed partial class HereticAimGunDoAfterEvent : SimpleDoAfterEvent;
}
