// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Numerics;
using Content.Shared._Shitmed.Targeting;
using Content.Shared._Shitmed.Weapons.Ranged.Events;
using Content.Shared.Camera;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Execution;
using Content.Shared.PneumaticCannon;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Trauma.Shared.Projectiles;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Executions;

/// <summary>
/// Verb for violently murdering cuffed creatures using guns.
/// Uses <see cref="AmmoImpactEvent"/> to pretend a projectile was fired then immediately hit the target.
/// This is basically copy of gun code except using <c>AmmoImpactEvent</c> instead of <c>CreateAndFireProjectiles</c>.
/// <see cref="BeingExecutedComponent"/> allows damage to the target to get multiplied while the execution is being processed.
/// </summary>
public sealed class GunExecutionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly PredictedProjectileSystem _projectile = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedExecutionSystem _execution = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrownItemSystem _thrownItem = default!;

    private EntityQuery<ProjectileComponent> _projectileQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _projectileQuery = GetEntityQuery<ProjectileComponent>();

        /* Interaction */
        SubscribeLocalEvent<GunComponent, GetVerbsEvent<UtilityVerb>>(OnGetVerbs);
        SubscribeLocalEvent<GunComponent, ExecutionDoAfterEvent>(OnDoAfter);

        /* AmmoImpactEvent */
        SubscribeLocalEvent<CartridgeAmmoComponent, AmmoImpactEvent>(OnCartridgeAmmoImpact);
        SubscribeLocalEvent<ProjectileSpreadComponent, AmmoImpactEvent>(OnSpreadAmmoImpact);
        SubscribeLocalEvent<HitscanAmmoComponent, AmmoImpactEvent>(OnHitscanAmmoImpact);

        /* Damage modifying */
        SubscribeLocalEvent<BeingExecutedComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<PneumaticCannonComponent, ModifyExecutionDamageEvent>(OnCannonModifyExecutionDamage);
    }

    #region Event handlers

    private void OnGetVerbs(Entity<GunComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Hands == null || args.Using != ent.Owner || !args.CanAccess || !args.CanInteract)
            return;

        var attacker = args.User;
        var victim = args.Target;

        if (!CanExecuteWithGun(victim, attacker, ent.Comp))
            return;

        UtilityVerb verb = new()
        {
            Act = () =>
            {
                TryStartDoAfter(ent, victim, attacker);
            },
            Impact = LogImpact.High,
            Text = Loc.GetString("execution-verb-name"),
            Message = Loc.GetString("execution-verb-message"),
        };

        args.Verbs.Add(verb);
    }

    private void OnDoAfter(Entity<GunComponent> weapon, ref ExecutionDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used != weapon.Owner || args.Target is not {} victim || !_timing.IsFirstTimePredicted)
            return;

        args.Handled = true;

        TryExecute(weapon, victim, args.User);
    }

    private void OnHitscanAmmoImpact(Entity<HitscanAmmoComponent> ent, ref AmmoImpactEvent args)
    {
        var data = new HitscanRaycastFiredData()
        {
            ShotDirection = GetDirection(args.Target, args.Shooter),
            HitEntity = args.Target,
            Gun = args.Weapon,
            Shooter = args.Shooter
        };
        var ev = new HitscanRaycastFiredEvent
        {
            Data = data
        };
        RaiseLocalEvent(ent, ref ev);
        PredictedQueueDel(ent);
        args.Handled = true; // hitscan entity does nothing with projectile hit events
    }

    private void OnCartridgeAmmoImpact(Entity<CartridgeAmmoComponent> ent, ref AmmoImpactEvent args)
    {
        args.Handled = true; // the cartridge won't do anything by itself
        if (ent.Comp.Spent)
        {
            args.Failed = true;
            return; // no shoot
        }

        ent.Comp.Spent = true;
        Dirty(ent);
        _appearance.SetData(ent.Owner, AmmoVisuals.Spent, true);

        var coords = Transform(args.Shooter).Coordinates;
        var projectile = PredictedSpawnAtPosition(ent.Comp.Prototype, coords);
        // now have the actual projectile impact the target
        // for most bullets this just does hit, shotguns will do it for each pellet
        DoImpact(args.Weapon, projectile, args.Shooter, args.Target);

        // done with the cartridge, now for caseless ammo
        if (ent.Comp.DeleteOnSpawn)
        {
            PredictedQueueDel(ent);
            return;
        }

        // Something like ballistic might want to leave it in the container still
        if (_container.IsEntityInContainer(ent))
            return;

        var direction = GetDirection(args.Target, args.Shooter);
        var angle = _gun.GetRecoilAngle(_timing.CurTime, args.Weapon, direction.ToAngle(), args.Shooter);
        _gun.EjectCartridge(_gun.Random(args.Weapon), args.Shooter, ent, angle);
    }

    private void OnSpreadAmmoImpact(Entity<ProjectileSpreadComponent> ent, ref AmmoImpactEvent args)
    {
        var proj = _projectileQuery.Comp(ent);
        var wasSpent = proj.ProjectileSpent;
        for (var i = 1; i < ent.Comp.Count; i++)
        {
            // reuse the same spread entity, this only works because every pellet spread prototype behaves like the pellets it spawns
            // however we need to reset spent so it doesn't just get ignored
            proj.ProjectileSpent = false;
            _projectile.DoHit(ent, args.Target);
        }
        proj.ProjectileSpent = wasSpent;
    }

    private void OnDamageModify(Entity<BeingExecutedComponent> ent, ref DamageModifyEvent args)
    {
        args.Damage *= ent.Comp.Modifier;
    }

    private void OnCannonModifyExecutionDamage(Entity<PneumaticCannonComponent> ent, ref ModifyExecutionDamageEvent args)
    {
        // fast knife go brr, slow knife bounces off you
        if (ent.Comp.ProjectileSpeed is {} speed)
            args.Modifier *= speed / ent.Comp.BaseProjectileSpeed;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Returns true if someone can be executed using a gun.
    /// </summary>
    public bool CanExecuteWithGun(EntityUid victim, EntityUid user, GunComponent gun)
    {
        if (!_execution.CanBeExecuted(victim, user))
            return false;

        // We must be able to actually fire the gun
        return _gun.CanShoot(gun);
    }

    /// <summary>
    /// Tries to start executing <c>victim</c> by <c>attacker</c>, using <c>weapon</c>.
    /// </summary>
    public bool TryStartDoAfter(Entity<GunComponent> weapon, EntityUid victim, EntityUid attacker)
    {
        if (!CanExecuteWithGun(victim, attacker, weapon.Comp))
            return false;

        var executionTime = weapon.Comp.ExecutionTime;
        var prefix = "execution";
        if (attacker == victim)
        {
            prefix = "suicide";
            executionTime = weapon.Comp.SuicideTime;
        }

        _execution.ShowExecutionInternalPopup(prefix + "-popup-gun-initial-internal", attacker, victim, weapon);
        _execution.ShowExecutionExternalPopup(prefix + "-popup-gun-initial-external", attacker, victim, weapon);

        var doAfter = new DoAfterArgs(EntityManager,
            attacker,
            executionTime,
            new ExecutionDoAfterEvent(),
            eventTarget: weapon,
            target: victim,
            used: weapon)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };

        return _doAfter.TryStartDoAfter(doAfter);
    }

    /// <summary>
    /// Immediately executes <c>victim</c> by <c>attacker</c> using <c>weapon</c> doing popups etc.
    /// Returns true if it succeeded.
    /// Succeeding doesn't necessarily mean the victim is dead, it could be a practice round etc.
    /// </summary>
    public bool TryExecute(Entity<GunComponent> weapon, EntityUid victim, EntityUid attacker)
    {
        if (!CanExecuteWithGun(victim, attacker, weapon.Comp))
            return false;

        // Check if any systems want to block our shot
        var prevention = new ShotAttemptedEvent
        {
            User = attacker,
            Used = weapon
        };

        RaiseLocalEvent(weapon, ref prevention);
        if (!prevention.Cancelled)
            RaiseLocalEvent(attacker, ref prevention);
        if (prevention.Cancelled)
            return false;

        // prevent shooting if e.g. pneumatic cannon has no tank
        var attemptEv = new AttemptShootEvent(attacker, null);
        RaiseLocalEvent(weapon, ref attemptEv);
        if (attemptEv.Cancelled)
        {
            if (attemptEv.Message is {} msg)
                _popup.PopupClient(msg, weapon, attacker);
            return false;
        }

        // Take some ammunition for the shot (one bullet)
        var fromCoordinates = Transform(attacker).Coordinates;
        var ev = new TakeAmmoEvent(1, new List<(EntityUid? Entity, IShootable Shootable)>(), fromCoordinates, attacker);
        RaiseLocalEvent(weapon, ev);

        // Check if there's any ammo left
        if (ev.Ammo.Count <= 0)
        {
            DoEmptyLogic(weapon, attacker, victim);
            return false;
        }

        if (ev.Ammo[0].Entity is not {} ammo)
        {
            Log.Error($"TakeAmmoEvent for {ToPrettyString(weapon)} gave invalid ammo!");
            return false;
        }

        // let clumsy etc. cancel it now
        var selfPrevention = new SelfBeforeGunShotEvent(attacker, weapon, ev.Ammo);
        RaiseLocalEvent(attacker, selfPrevention);
        if (selfPrevention.Cancelled)
            return false;

        // execution is a go, increase damage for the impact
        var modifyEv = new ModifyExecutionDamageEvent(weapon.Comp.ExecutionModifier);
        RaiseLocalEvent(weapon, ref modifyEv);
        var executed = EnsureComp<BeingExecutedComponent>(victim);
        executed.Modifier = modifyEv.Modifier;
        // TODO: set target part

        // target head for this because that's what the loc strings say
        var oldTarget = TargetBodyPart.Head;
        if (TryComp<TargetingComponent>(attacker, out var targeting))
        {
            oldTarget = targeting.Target;
            targeting.Target = TargetBodyPart.Head;
        }

        bool success;
        // incase there's an exception
        try
        {
            success = DoImpact(weapon, ammo, attacker, victim);
        }
        finally
        {
            // never leave gamer damage on surviving/revived victim
            RemComp(victim, executed);
            // restore target part as well
            targeting?.Target = oldTarget;
        }

        // popups and sounds
        if (success)
            DoShotLogic(weapon, attacker, victim, ev.Ammo);
        else
            DoEmptyLogic(weapon, attacker, victim);

        return success;
    }

    /// <summary>
    /// Fake an impact with the bullet/item.
    /// </summary>
    public bool DoImpact(Entity<GunComponent> gun, EntityUid ammo, EntityUid shooter, EntityUid target)
    {
        // let special ammo handle it
        var ev = new AmmoImpactEvent(gun, shooter, target);
        RaiseLocalEvent(ammo, ref ev);

        if (!ev.Handled)
        {
            if (_projectileQuery.HasComp(ammo))
                _projectile.DoHit(ammo, target); // bullets
            else
                DoThrowHit(ammo, shooter, target); // knives (pneumatic cannon)
        }

        return !ev.Failed;
    }

    /// <summary>
    /// Fake an impact with a thrown item aka non projectile.
    /// </summary>
    public void DoThrowHit(EntityUid ammo, EntityUid shooter, EntityUid target)
    {
        var thrown = EnsureComp<ThrownItemComponent>(ammo);
        thrown.Thrower = shooter;
        _thrownItem.ThrowCollideInteraction(thrown, ammo, target);
        RemComp(ammo, thrown);
    }

    #endregion

    #region Helper methods

    private Vector2 GetDirection(EntityUid target, EntityUid shooter)
    {
        var targetPos = _transform.GetMapCoordinates(target).Position;
        var shooterPos = _transform.GetMapCoordinates(shooter).Position;
        var direction = targetPos - shooterPos;
        if (direction != Vector2.Zero)
            direction /= direction.Length();
        return direction;
    }

    private void DoEmptyLogic(Entity<GunComponent> gun, EntityUid attacker, EntityUid victim)
    {
        var ev = new OnEmptyGunShotEvent(attacker);
        RaiseLocalEvent(gun, ref ev);
        _audio.PlayPredicted(gun.Comp.SoundEmpty, gun, attacker);
        _execution.ShowExecutionInternalPopup("execution-popup-gun-empty", attacker, victim, gun);
        _execution.ShowExecutionExternalPopup("execution-popup-gun-empty", attacker, victim, gun);
    }

    private void DoShotLogic(Entity<GunComponent> gun, EntityUid attacker, EntityUid victim, List<(EntityUid? Uid, IShootable Shootable)> ammo)
    {
        var ev = new GunShotEvent(attacker, ammo);
        RaiseLocalEvent(gun, ref ev);
        var userEv = new GunShotBodyEvent(gun, gun.Comp);
        RaiseLocalEvent(attacker, userEv);

        _audio.PlayPredicted(gun.Comp.SoundGunshot, gun, attacker);
        var direction = GetDirection(target: victim, shooter: attacker);
        // do muzzle flash for bullets and stuff
        if (ammo[0].Shootable is AmmoComponent ammoComp)
            _gun.MuzzleFlash(gun, ammoComp, direction.ToAngle(), attacker);

        // special text for suicide
        var prefix = "suicide";
        if (attacker != victim)
        {
            DoRecoil(attacker, victim, direction);
            prefix = "execution";
        }

        _execution.ShowExecutionInternalPopup(prefix + "-popup-gun-complete-internal", attacker, victim, gun);
        _execution.ShowExecutionExternalPopup(prefix + "-popup-gun-complete-external", attacker, victim, gun);
    }

    private void DoRecoil(EntityUid attacker, EntityUid victim, Vector2 direction)
    {
        // client predicts it instead
        if (_net.IsServer)
            return;

        // opposite direction for recoil
        if (direction != Vector2.Zero)
            _recoil.KickCamera(attacker, -direction);
    }

    #endregion
}
