using System.Numerics;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Camera;
using Content.Shared.Clumsy;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Execution;
using Content.Shared.Explosion.Components;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.PneumaticCannon;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Executions;

/// <summary>
/// Verb for violently murdering cuffed creatures using guns.
/// </summary>
public sealed class ExecutionSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedExecutionSystem _execution = default!;
    [Dependency] private readonly SharedExplosionSystem _explosion = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunComponent, GetVerbsEvent<UtilityVerb>>(OnGetInteractionVerbsGun);
        SubscribeLocalEvent<GunComponent, ExecutionDoAfterEvent>(OnDoafterGun);

        SubscribeLocalEvent<CartridgeAmmoComponent, TakeAmmoGetDamageFromProjectileEvent>(OnTakeAmmoCartridge);
        SubscribeLocalEvent<HitscanBasicDamageComponent, TakeAmmoGetDamageFromProjectileEvent>(OnTakeAmmoHitscanBasic);
        SubscribeLocalEvent<ProjectileComponent, TakeAmmoGetDamageFromProjectileEvent>(OnTakeAmmoProjectile);
    }

    private void OnTakeAmmoHitscanBasic(Entity<HitscanBasicDamageComponent> ent, ref TakeAmmoGetDamageFromProjectileEvent args)
    {
        args.Damage = ent.Comp.Damage;
    }

    private void OnTakeAmmoProjectile(Entity<ProjectileComponent> ent, ref TakeAmmoGetDamageFromProjectileEvent args)
    {
        args.Damage = ent.Comp.Damage;
    }

    private void OnTakeAmmoCartridge(Entity<CartridgeAmmoComponent> ent, ref TakeAmmoGetDamageFromProjectileEvent args)
    {
        if (ent.Comp.Spent)
        {
            args.Cancelled = true;
            return;
        }

        ent.Comp.Spent = true;
        _appearance.SetData(ent, AmmoVisuals.Spent, true);
        Dirty(ent, ent.Comp);

        var prototype = _proto.Index(ent.Comp.Prototype);

        if (prototype.TryGetComponent<ProjectileComponent>(out var projectileA, Factory)) // sloth forgive me
            args.Damage = projectileA.Damage;

        if (prototype.TryGetComponent<ProjectileSpreadComponent>(out var projectileSpread, Factory)) // sloth forgive me
            args.Damage *= projectileSpread.Count;

        args.Delete = false;
    }

    private void OnGetInteractionVerbsGun(Entity<GunComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Hands == null || args.Using is not {} weapon || !args.CanAccess || !args.CanInteract)
            return;

        var attacker = args.User;
        var victim = args.Target;

        if (!CanExecuteWithGun(victim, attacker, ent.Comp))
            return;

        UtilityVerb verb = new()
        {
            Act = () =>
            {
                TryStartGunExecutionDoafter((weapon, ent.Comp), victim, attacker, ent.Comp);
            },
            Impact = LogImpact.High,
            Text = Loc.GetString("execution-verb-name"),
            Message = Loc.GetString("execution-verb-message"),
        };

        args.Verbs.Add(verb);
    }

    private bool CanExecuteWithGun(EntityUid victim, EntityUid user, GunComponent guncomp)
    {
        if (!_execution.CanBeExecuted(victim, user))
            return false;

        // We must be able to actually fire the gun
        return _gun.CanShoot(guncomp);
    }

    private void TryStartGunExecutionDoafter(Entity<GunComponent> weapon, EntityUid victim, EntityUid attacker, GunComponent guncomp)
    {
        if (!CanExecuteWithGun(victim, attacker, guncomp))
            return;

        var executionTime = weapon.Comp.ExecutionTime;

        string prefix = "execution";

        if (attacker == victim)
        {
            prefix = "suicide";
            executionTime = weapon.Comp.SuicideTime;
        }

        _execution.ShowExecutionInternalPopup(prefix + "-popup-gun-initial-internal", attacker, victim, weapon);
        _execution.ShowExecutionExternalPopup(prefix + "-popup-gun-initial-external", attacker, victim, weapon);

        var doAfter =
            new DoAfterArgs(EntityManager, attacker, executionTime, new ExecutionDoAfterEvent(), weapon, target: victim, used: weapon)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnDoafterGun(EntityUid uid, GunComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used == null || args.Target == null || !_timing.IsFirstTimePredicted)
            return;

        var attacker = args.User;
        var weapon = args.Used!.Value;
        var gunComp = Comp<GunComponent>(weapon);

        var victim = args.Target!.Value;

        if (!CanExecuteWithGun(victim, attacker, component))
            return;

        // Check if any systems want to block our shot
        var prevention = new ShotAttemptedEvent
        {
            User = attacker,
            Used = new Entity<GunComponent>(weapon, gunComp)
        };

        RaiseLocalEvent(weapon, ref prevention);
        if (prevention.Cancelled)
            return;

        RaiseLocalEvent(attacker, ref prevention);
        if (prevention.Cancelled)
            return;

        // Not sure what this is for but gunsystem uses it so ehhh
        var attemptEv = new AttemptShootEvent(attacker, null);
        RaiseLocalEvent(weapon, ref attemptEv);

        if (attemptEv.Cancelled)
        {
            if (attemptEv.Message != null)
            {
                _popup.PopupClient(attemptEv.Message, weapon, attacker);
            }
            return;
        }

        // Get the direction for the recoil
        Vector2 direction = Vector2.Zero;
        var attackerXform = _transform.GetWorldPosition(attacker);
        var victimXform = _transform.GetWorldPosition(victim);
        var diff = victimXform - attackerXform;
        if (diff != Vector2.Zero)
            direction = -diff.Normalized(); // recoil opposite of shot

        // Take some ammunition for the shot (one bullet)
        var fromCoordinates = Transform(attacker).Coordinates;
        var ev = new TakeAmmoEvent(1, new List<(EntityUid? Entity, IShootable Shootable)>(), fromCoordinates, attacker);
        RaiseLocalEvent(weapon, ev);

        // Check if there's any ammo left
        if (ev.Ammo.Count <= 0)
        {
            DoEmptyGunLogic(component, weapon, attacker, victim);
            return;
        }

        var selfPrevention = new SelfBeforeGunShotEvent(attacker, (weapon, gunComp), ev.Ammo);

        RaiseLocalEvent(attacker, selfPrevention);
        if (selfPrevention.Cancelled)
            return;

        // Information about the ammo like damage
        DamageSpecifier damage = new DamageSpecifier();

        // Get some information from IShootable

        if (ev.Ammo[0].Entity is not {} ammo)
            return;

        // Explode if the projective is explosive for mgsGZ helicopter scene parody
        if (TryComp<ExplosiveComponent>(ammo, out var explosive))
        {
            _explosion.QueueExplosion(ammo, explosive.ExplosionType, explosive.TotalIntensity, explosive.IntensitySlope, explosive.MaxIntensity, canCreateVacuum: explosive.CanCreateVacuum);
        }

        var ammoEvent = new TakeAmmoGetDamageFromProjectileEvent(damage, true);
        RaiseLocalEvent(ammo, ref ammoEvent);

        if (ammoEvent.Cancelled)
        {
            DoEmptyGunLogic(component, weapon, attacker, victim);
            return;
        }

        damage = ammoEvent.Damage;

        if (ammoEvent.Delete)
            PredictedDel(ammo);

        var selfEvent = new SelfBeforeGunShotEvent(attacker, (weapon, gunComp), ev.Ammo);
        RaiseLocalEvent(attacker, selfEvent);
        if (selfEvent.Cancelled && !component.ClumsyProof && HasComp<ClumsyComponent>(attacker))
        {
            // You shoot yourself with the gun (no damage multiplier)
            _damageable.TryChangeDamage(attacker, damage, origin: attacker);
        }

        if (selfEvent.Cancelled)
            return;

        if (TryComp<PneumaticCannonComponent>(weapon, out var pneumaticCannonComponent) && pneumaticCannonComponent.ProjectileSpeed != null)
            damage *= pneumaticCannonComponent.ProjectileSpeed.Value;

        _damageable.TryChangeDamage(victim, damage * component.ExecutionModifier, true, targetPart: TargetBodyPart.Head);

        _audio.PlayPredicted(component.SoundGunshot, weapon, attacker);

        // Popups
        string prefix = "suicide";
        if (attacker != victim)
        {
            if (_net.IsClient && direction != Vector2.Zero)
                _recoil.KickCamera(attacker, direction);
            prefix = "execution";
        }

        _execution.ShowExecutionInternalPopup(prefix + "-popup-gun-complete-internal", attacker, victim, weapon);
        _execution.ShowExecutionExternalPopup(prefix + "-popup-gun-complete-external", attacker, victim, weapon);
    }

    private void DoEmptyGunLogic(GunComponent guncomp, EntityUid weapon, EntityUid attacker, EntityUid victim)
    {
        _audio.PlayPredicted(guncomp.SoundEmpty, weapon, attacker);
        _execution.ShowExecutionInternalPopup("execution-popup-gun-empty", attacker, victim, weapon);
        _execution.ShowExecutionExternalPopup("execution-popup-gun-empty", attacker, victim, weapon);
    }
}
