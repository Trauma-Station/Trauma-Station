using System.Numerics;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.ActionBlocker;
using Content.Shared.Camera;
using Content.Shared.Clumsy;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Execution;
using Content.Shared.Explosion.Components;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Mobs.Components;
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
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared;

/// <summary>
/// Verb for violently murdering cuffed creatures using guns.
/// </summary>
public sealed class ExecutionSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedExecutionSystem _execution = default!;
    [Dependency] private readonly SharedExplosionSystem _explosionSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunComponent, GetVerbsEvent<UtilityVerb>>(OnGetInteractionVerbsGun);

        SubscribeLocalEvent<GunComponent, ExecutionDoAfterEvent>(OnDoafterGun);
    }

    private void OnGetInteractionVerbsGun(
        EntityUid uid,
        GunComponent component,
        GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Hands == null || args.Using == null || !args.CanAccess || !args.CanInteract)
            return;

        var attacker = args.User;
        var weapon = args.Using!.Value;
        var victim = args.Target;

        if (!CanExecuteWithGun(weapon, victim, attacker))
            return;

        UtilityVerb verb = new()
        {
            Act = () =>
            {
                TryStartGunExecutionDoafter((weapon, component), victim, attacker); // Mono - pass in component
            },
            Impact = LogImpact.High,
            Text = Loc.GetString("execution-verb-name"),
            Message = Loc.GetString("execution-verb-message"),
        };

        args.Verbs.Add(verb);
    }

    private bool CanExecuteWithAny(EntityUid weapon, EntityUid victim, EntityUid attacker)
    {
        // No point executing someone if they can't take damage
        if (!TryComp<DamageableComponent>(victim, out _))
            return false;

        // You can't execute something that cannot die
        if (!TryComp<MobStateComponent>(victim, out _))
            return false;

        // You must be able to attack people to execute
        if (!_actionBlockerSystem.CanAttack(attacker, victim))
            return false;

        // The victim must be incapacitated to be executed
        if (victim != attacker && _actionBlockerSystem.CanInteract(victim, null))
            return false;

        // All checks passed
        return true;
    }

    private bool CanExecuteWithGun(EntityUid weapon, EntityUid victim, EntityUid user)
    {
        if (!_execution.CanBeExecuted(victim, user))
            return false;

        // We must be able to actually fire the gun
        if (!TryComp<GunComponent>(weapon, out var gun) || !_gunSystem.CanShoot(gun!))
            return false;

        return true;
    }

    private void TryStartGunExecutionDoafter(Entity<GunComponent> weapon, EntityUid victim, EntityUid attacker)
    {
        if (!CanExecuteWithGun(weapon, victim, attacker))
            return;

        var executionTime = weapon.Comp.ExecutionTime;

        if (attacker == victim)
        {
            _execution.ShowExecutionInternalPopup("suicide-popup-gun-initial-internal", attacker, victim, weapon);
            _execution.ShowExecutionExternalPopup("suicide-popup-gun-initial-external", attacker, victim, weapon);
            executionTime = weapon.Comp.SuicideTime;
        }
        else
        {
            _execution.ShowExecutionInternalPopup("execution-popup-gun-initial-internal", attacker, victim, weapon);
            _execution.ShowExecutionExternalPopup("execution-popup-gun-initial-external", attacker, victim, weapon);
        }

        var doAfter =
            new DoAfterArgs(EntityManager, attacker, executionTime, new ExecutionDoAfterEvent(), weapon, target: victim, used: weapon) // Mono - GunExecutionTime -> executionTime
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };

        _doAfterSystem.TryStartDoAfter(doAfter);
    }

    private void OnDoafterGun(EntityUid uid, GunComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used == null || args.Target == null || !_timing.IsFirstTimePredicted)
            return;

        var attacker = args.User;
        var weapon = args.Used!.Value;
        var gunComp = Comp<GunComponent>(weapon);

        var victim = args.Target!.Value;

        if (!CanExecuteWithGun(weapon, victim, attacker)) return;

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
                _popupSystem.PopupClient(attemptEv.Message, weapon, attacker);
                return;
            }
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
        var ev = new TakeAmmoEvent(1, new List<(EntityUid? Entity, IShootable Shootable)>(), fromCoordinates, attacker); // Mono - willBeFired
        RaiseLocalEvent(weapon, ev);

        // Check if there's any ammo left
        if (ev.Ammo.Count <= 0)
        {
            _audioSystem.PlayPredicted(component.SoundEmpty, weapon, attacker);
            _execution.ShowExecutionInternalPopup("execution-popup-gun-empty", attacker, victim, weapon);
            return;
        }

        // Information about the ammo like damage
        DamageSpecifier damage = new DamageSpecifier();
        var count = 1;

        // Get some information from IShootable
        var ammoUid = ev.Ammo[0].Entity;


        if (TryComp<ProjectileSpreadComponent>(ammoUid, out var projectilespread))
        {
            count = projectilespread.Count;
        }

        // Explode if the projective is explosive for mgsGZ helicopter scene parody
        if (TryComp<ExplosiveComponent>(ammoUid, out var explosive))
        {
            _explosionSystem.QueueExplosion(ammoUid.Value, explosive.ExplosionType, explosive.TotalIntensity, explosive.IntensitySlope, explosive.MaxIntensity, canCreateVacuum: explosive.CanCreateVacuum);
        }

        switch (ev.Ammo[0].Shootable)
        {
            case CartridgeAmmoComponent cartridge:
                // Get the damage value
                var prototype = _prototypeManager.Index<EntityPrototype>(cartridge.Prototype);
                prototype.TryGetComponent<ProjectileComponent>(out var projectileA, _componentFactory); // sloth forgive me
                if (projectileA != null)
                    damage = projectileA.Damage;

                // Expend the cartridge
                cartridge.Spent = true;
                _appearanceSystem.SetData(ammoUid!.Value, AmmoVisuals.Spent, true);
                Dirty(ammoUid.Value, cartridge);

                break;

            case AmmoComponent:
                if (TryComp<ProjectileComponent>(ammoUid, out var projectileB))
                    damage = projectileB.Damage;

                Del(ammoUid);
                break;

            case HitscanAmmoComponent:
                if (TryComp<HitscanBasicDamageComponent>(ammoUid, out var hitscanDamage))
                    damage = hitscanDamage.Damage;

                Del(ammoUid);
                break;

            default:
                throw new InvalidOperationException($"Unknown shootable type [{ev.Ammo[0].Shootable}]");
        }
        // Clumsy people have a chance to shoot themselves (not in the head)
        if (!component.ClumsyProof &&
            TryComp<ClumsyComponent>(attacker, out var clumsy) && _random.Prob(clumsy.ClumsyDefaultCheck))
        {
            _execution.ShowExecutionInternalPopup("execution-popup-gun-clumsy-internal", attacker, victim, weapon);
            _execution.ShowExecutionExternalPopup("execution-popup-gun-clumsy-external", attacker, victim, weapon);

            // You shoot yourself with the gun (no damage multiplier)
            _damageableSystem.TryChangeDamage(attacker, damage, origin: attacker);
            _audioSystem.PlayPredicted(component.SoundGunshot, weapon, attacker);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            _damageableSystem.TryChangeDamage(victim, damage * component.ExecutionModifier, true, targetPart: TargetBodyPart.Head); // Mono - ExecutionModifier
        }
        _audioSystem.PlayPredicted(component.SoundGunshot, weapon, attacker);

        // Popups
        if (attacker != victim)
        {
            if (_net.IsClient && direction != Vector2.Zero)
                _recoil.KickCamera(attacker, direction);
            _execution.ShowExecutionInternalPopup("execution-popup-gun-complete-internal", attacker, victim, weapon);
            _execution.ShowExecutionExternalPopup("execution-popup-gun-complete-external", attacker, victim, weapon);
        }
        else
        {
            _execution.ShowExecutionInternalPopup("suicide-popup-gun-complete-internal", attacker, victim, weapon);
            _execution.ShowExecutionExternalPopup("suicide-popup-gun-complete-external", attacker, victim, weapon);
        }
    }
}
