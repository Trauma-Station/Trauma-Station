// <Trauma>
using Content.Shared.Damage.Components;
using Content.Shared.Tag;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;
// </Trauma>
using System.Numerics;
using Content.Goobstation.Common.Projectiles;
using Content.Goobstation.Common.Weapons.Penetration;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Damage;
using Content.Shared._RMC14.Weapons.Ranged.Prediction;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Administration.Logs;
using Content.Shared.Camera;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Effects;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.Projectiles;

public abstract partial class SharedProjectileSystem : EntitySystem
{
    public const string ProjectileFixture = "projectile";

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedGunSystem _guns = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _sharedCameraRecoil = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly TagSystem _tag = default!; // Goob

    private static readonly ProtoId<TagPrototype> GunCanAimShooterTag = "GunCanAimShooter"; // Goob

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProjectileComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<ProjectileComponent, PreventCollideEvent>(PreventCollision);
        SubscribeLocalEvent<EmbeddableProjectileComponent, PreventCollideEvent>(EmbeddablePreventCollision); // Goobstation - Crawl Fix
        SubscribeLocalEvent<EmbeddableProjectileComponent, ProjectileHitEvent>(OnEmbedProjectileHit);
        SubscribeLocalEvent<EmbeddableProjectileComponent, ThrowDoHitEvent>(OnEmbedThrowDoHit);
        SubscribeLocalEvent<EmbeddableProjectileComponent, ActivateInWorldEvent>(OnEmbedActivate);
        SubscribeLocalEvent<EmbeddableProjectileComponent, RemoveEmbeddedProjectileEvent>(OnEmbedRemove);
        SubscribeLocalEvent<EmbeddableProjectileComponent, ComponentShutdown>(OnEmbeddableCompShutdown);

        SubscribeLocalEvent<EmbeddedContainerComponent, EntityTerminatingEvent>(OnEmbeddableTermination);
    }

    private void OnStartCollide(EntityUid uid, ProjectileComponent component, ref StartCollideEvent args)
    {
        // This is so entities that shouldn't get a collision are ignored.
        if (args.OurFixtureId != ProjectileFixture || !args.OtherFixture.Hard
            || component.DamagedEntity || component is { Weapon: null, OnlyCollideWhenShot: true })
            return;

        ProjectileCollide((uid, component, args.OurBody), args.OtherEntity);
    }

    public void ProjectileCollide(Entity<ProjectileComponent, PhysicsComponent> projectile, EntityUid target, bool predicted = false)
    {
        ProjectileCollide(projectile, target, null, predicted);
    }

    public void ProjectileCollide(Entity<ProjectileComponent, PhysicsComponent> projectile, EntityUid target, MapCoordinates? collisionCoordinates, bool predicted = false)
    {
        var (uid, component, ourBody) = projectile;
        if (projectile.Comp1.DamagedEntity)
        {
            if (component.DeleteOnCollide)
                PredictedQueueDel(uid);

            return;
        }

        // it's here so this check is only done once before possible hit
        var attemptEv = new ProjectileReflectAttemptEvent(uid, component, false);
        RaiseLocalEvent(target, ref attemptEv);
        if (attemptEv.Cancelled)
        {
            SetShooter(uid, component, target);
            _guns.SetTarget(uid, null, out _); // Goobstation
            component.IgnoredEntities.Clear(); // Goobstation
            return;
        }

        var ev = new ProjectileHitEvent(component.Damage, target, component.Shooter);
        RaiseLocalEvent(uid, ref ev);
        if (ev.Handled)
            return;

        var coordinates = collisionCoordinates != null
            ? _transform.ToCoordinates(collisionCoordinates.Value)
            : Transform(projectile).Coordinates;
        var otherName = ToPrettyString(target);
        var damageRequired = GetDestructionDamage(target);
        if (TryComp<DamageableComponent>(target, out var damageableComponent))
        {
            damageRequired -= damageableComponent.TotalDamage;
            damageRequired = FixedPoint2.Max(damageRequired, FixedPoint2.Zero);
        }

        var direction = ourBody.LinearVelocity.Normalized();
        // <Goob>
        TargetBodyPart? targetPart = null;
        if (TryComp(uid, out ProjectileMissTargetPartChanceComponent? missComp) &&
            !missComp.PerfectHitEntities.Contains(target))
            targetPart = TargetBodyPart.Chest;
        var modifiedDamage = _net.IsClient
            ? new DamageSpecifier(ev.Damage)
            : _damageableSystem.TryChangeDamage(target,
                ev.Damage,
                out var damage,
                component.IgnoreResistances,
                origin: component.Shooter,
                targetPart: targetPart,
                tool: uid)
                ? damage
                : null;
        // </Goob>
        var deleted = Deleted(target);

        var filter = Filter.Pvs(coordinates, entityMan: EntityManager);
        if (_guns.GunPrediction &&
            TryComp(projectile, out PredictedProjectileServerComponent? serverProjectile) &&
            serverProjectile.Shooter is { } shooter)
        {
            filter = filter.RemovePlayer(shooter);
        }

        if (modifiedDamage is not null && (EntityManager.EntityExists(component.Shooter) || EntityManager.EntityExists(component.Weapon)))
        {
            if (modifiedDamage.AnyPositive() && !deleted)
            {
                _color.RaiseEffect(Color.Red, new List<EntityUid> { target }, filter);
            }

            var shooterOrWeapon = EntityManager.EntityExists(component.Shooter) ? component.Shooter!.Value : component.Weapon!.Value;
            var total = modifiedDamage.GetTotal();

            _adminLogger.Add(LogType.BulletHit,
                HasComp<ActorComponent>(target) ? LogImpact.Extreme : LogImpact.High,
                $"Projectile {ToPrettyString(uid):projectile} shot by {ToPrettyString(shooterOrWeapon):source} hit {otherName:target} and dealt {total:damage} damage");

            // <Goob> - Splits penetration change if target have PenetratableComponent
            if (!TryComp<PenetratableComponent>(target, out var penetratable))
            {
                // If the object won't be destroyed, it "tanks" the penetration hit.
                if (total < damageRequired)
                {
                    component.ProjectileSpent = true;
                }

                if (!component.ProjectileSpent)
                {
                    component.PenetrationAmount += damageRequired;
                    // The projectile has dealt enough damage to be spent.
                    if (component.PenetrationAmount >= component.PenetrationThreshold)
                    {
                        component.ProjectileSpent = true;
                    }
                }
            }
            else
            {
                // Goobstation - Here penetration threshold count as "penetration health".
                // If it's lower than damage than penetation damage entity cause it deletes projectile
                if (component.PenetrationThreshold < penetratable.PenetrateDamage)
                {
                    component.ProjectileSpent = true;
                }

                component.PenetrationThreshold -= FixedPoint2.New(penetratable.PenetrateDamage);
                component.Damage *= (1 - penetratable.DamagePenaltyModifier);
            }
            // </Goob>

            // If penetration is to be considered, we need to do some checks to see if the projectile should stop.
            if (component.PenetrationThreshold != 0)
            {
                // If a damage type is required, stop the bullet if the hit entity doesn't have that type.
                if (component.PenetrationDamageTypeRequirement != null)
                {
                    var stopPenetration = false;
                    foreach (var requiredDamageType in component.PenetrationDamageTypeRequirement)
                    {
                        if (!modifiedDamage.DamageDict.Keys.Contains(requiredDamageType))
                        {
                            stopPenetration = true;
                            break;
                        }
                    }
                    if (stopPenetration)
                        component.ProjectileSpent = true;
                }

                // If the object won't be destroyed, it "tanks" the penetration hit.
                if (total < damageRequired)
                {
                    component.ProjectileSpent = true;
                }

                if (!component.ProjectileSpent)
                {
                    component.PenetrationAmount += damageRequired;
                    // The projectile has dealt enough damage to be spent.
                    if (component.PenetrationAmount >= component.PenetrationThreshold)
                    {
                        component.ProjectileSpent = true;
                    }
                }
            }
            else
            {
                component.ProjectileSpent = true;
            }
        }

        // Goobstation start
        if (component.Penetrate)
        {
            component.IgnoredEntities.Add(target);
            component.ProjectileSpent = false; // Hardlight bow should be able to deal damage while piercing, no?
        }
        // Goobstation end

        if (!deleted)
        {
            _guns.PlayImpactSound(target, modifiedDamage, component.SoundHit, component.ForceSound, filter, projectile);
            _sharedCameraRecoil.KickCamera(target, direction);
        }

        component.DamagedEntity = true;
        Dirty(uid, component);

        var noPenetration = TryComp(target, out PhysicsComponent? physics) &&
                            (component.NoPenetrateMask & physics.CollisionLayer) != 0;

        if (!predicted && component is { DeleteOnCollide: true, ProjectileSpent: true } || noPenetration) // Goobstation - Make x-ray arrows not penetrate blob
            PredictedQueueDel(uid);
        else if (_net.IsServer && component.DeleteOnCollide)
        {
            var predictedComp = EnsureComp<PredictedProjectileHitComponent>(uid);
            predictedComp.Origin = _transform.GetMoverCoordinates(coordinates);

            var targetCoords = _transform.GetMoverCoordinates(target);
            if (predictedComp.Origin.TryDistance(EntityManager, _transform, targetCoords, out var distance))
                predictedComp.Distance = distance;

            Dirty(uid, predictedComp);
        }

        if ((_net.IsServer || IsClientSide(uid)) && component.ImpactEffect != null)
        {
            var impactEffectEv = new ImpactEffectEvent(component.ImpactEffect, GetNetCoordinates(coordinates));
            if (_net.IsServer)
                RaiseNetworkEvent(impactEffectEv, filter);
            else
                RaiseLocalEvent(impactEffectEv);
        }
    }

    protected virtual FixedPoint2 GetDestructionDamage(EntityUid target)
    {
        return FixedPoint2.Zero;
    }

    private void OnEmbedActivate(Entity<EmbeddableProjectileComponent> embeddable, ref ActivateInWorldEvent args)
    {
        // Unremovable embeddables moment
        if (embeddable.Comp.RemovalTime == null)
            return;

        if (args.Handled || !args.Complex || !TryComp<PhysicsComponent>(embeddable, out var physics) ||
            physics.BodyType != BodyType.Static)
            return;

        args.Handled = true;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            embeddable.Comp.RemovalTime.Value,
            new RemoveEmbeddedProjectileEvent(),
            eventTarget: embeddable,
            target: embeddable));
    }

    private void OnEmbedRemove(Entity<EmbeddableProjectileComponent> embeddable, ref RemoveEmbeddedProjectileEvent args)
    {
        if (args.Cancelled)
            return;

        EmbedDetach(embeddable, embeddable.Comp, args.User);

        // try place it in the user's hand
        _hands.TryPickupAnyHand(args.User, embeddable);
    }

    private void OnEmbeddableCompShutdown(Entity<EmbeddableProjectileComponent> embeddable, ref ComponentShutdown arg)
    {
        EmbedDetach(embeddable, embeddable.Comp);
    }

    private void OnEmbedThrowDoHit(Entity<EmbeddableProjectileComponent> embeddable, ref ThrowDoHitEvent args)
    {
        if (!embeddable.Comp.EmbedOnThrow)
            return;

        EmbedAttach(embeddable, args.Target, null, embeddable.Comp);
    }

    private void OnEmbedProjectileHit(Entity<EmbeddableProjectileComponent> embeddable, ref ProjectileHitEvent args)
    {
        EmbedAttach(embeddable, args.Target, args.Shooter, embeddable.Comp);

        // Raise a specific event for projectiles.
        if (!TryComp<ProjectileComponent>(embeddable, out var projectile))
            return;

        var ev = new ProjectileEmbedEvent(projectile.Shooter, projectile.Weapon, args.Target);
        RaiseLocalEvent(embeddable, ref ev);
    }

    private void EmbedAttach(EntityUid uid, EntityUid target, EntityUid? user, EmbeddableProjectileComponent component)
    {
        TryComp<PhysicsComponent>(uid, out var physics);
        _physics.SetLinearVelocity(uid, Vector2.Zero, body: physics);
        _physics.SetBodyType(uid, BodyType.Static, body: physics);
        var xform = Transform(uid);
        _transform.SetParent(uid, xform, target);

        if (component.Offset != Vector2.Zero)
        {
            var rotation = xform.LocalRotation;
            if (TryComp<ThrowingAngleComponent>(uid, out var throwingAngleComp))
                rotation += throwingAngleComp.Angle;
            _transform.SetLocalPosition(uid, xform.LocalPosition + rotation.RotateVec(component.Offset), xform);
        }

        _audio.PlayPredicted(component.Sound, uid, null);
        component.EmbeddedIntoUid = target;
        var ev = new EmbedEvent(user, target);
        RaiseLocalEvent(uid, ref ev);
        Dirty(uid, component);

        EnsureComp<EmbeddedContainerComponent>(target, out var embeddedContainer);

        // <Trauma> - just warn instead of assert and have a useful message
        if (embeddedContainer.EmbeddedObjects.Contains(uid))
            Log.Warning($"Entity {ToPrettyString(uid)} was not supposed to be embedded into {ToPrettyString(target)}!");
        // </Trauma>

        embeddedContainer.EmbeddedObjects.Add(uid);
    }

    public void EmbedDetach(EntityUid uid, EmbeddableProjectileComponent? component, EntityUid? user = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.EmbeddedIntoUid == null)
            return; // the entity is not embedded, so do nothing

        if (TryComp<EmbeddedContainerComponent>(component.EmbeddedIntoUid.Value, out var embeddedContainer))
        {
            embeddedContainer.EmbeddedObjects.Remove(uid);
            Dirty(component.EmbeddedIntoUid.Value, embeddedContainer);
            if (embeddedContainer.EmbeddedObjects.Count == 0)
                RemCompDeferred<EmbeddedContainerComponent>(component.EmbeddedIntoUid.Value);
        }

        if (component.DeleteOnRemove)
        {
            PredictedQueueDel(uid);
            return;
        }

        var xform = Transform(uid);
        if (TerminatingOrDeleted(xform.GridUid) && TerminatingOrDeleted(xform.MapUid))
            return;
        TryComp<PhysicsComponent>(uid, out var physics);
        _physics.SetBodyType(uid, BodyType.Dynamic, body: physics, xform: xform);
        _transform.AttachToGridOrMap(uid, xform);
        component.EmbeddedIntoUid = null;
        Dirty(uid, component);

        // Reset whether the projectile has damaged anything if it successfully was removed
        if (TryComp<ProjectileComponent>(uid, out var projectile))
        {
            projectile.Shooter = null;
            projectile.Weapon = null;
            projectile.ProjectileSpent = false;

            Dirty(uid, projectile);
        }

        if (user != null)
        {
            // Land it just coz uhhh yeah
            var landEv = new LandEvent(user, true);  // note from goobstation: if this line is removed, syringe gun will break, LOOK AT THIS IF YOU ARE SEEING THIS IN A MERGE CONFLICT
            RaiseLocalEvent(uid, ref landEv);
        }

        _physics.WakeBody(uid, body: physics);
    }

    private void OnEmbeddableTermination(Entity<EmbeddedContainerComponent> container, ref EntityTerminatingEvent args)
    {
        DetachAllEmbedded(container);
    }

    public void DetachAllEmbedded(Entity<EmbeddedContainerComponent> container)
    {
        foreach (var embedded in container.Comp.EmbeddedObjects)
        {
            if (!TryComp<EmbeddableProjectileComponent>(embedded, out var embeddedComp))
                continue;

            EmbedDetach(embedded, embeddedComp);
        }
    }

    private void PreventCollision(EntityUid uid, ProjectileComponent component, ref PreventCollideEvent args)
    {
        // Goobstation - Crawling fix
        if (TryComp<RequireProjectileTargetComponent>(args.OtherEntity, out var requireTarget) && requireTarget.IgnoreThrow && requireTarget.Active)
            return;

        if (component.IgnoredEntities.Contains(args.OtherEntity))
        {
            args.Cancelled = true;
            return;
        }

        if ((component.Shooter == args.OtherEntity || component.Weapon == args.OtherEntity) &&
            component.Weapon != null && _tag.HasTag(component.Weapon.Value, GunCanAimShooterTag) &&
            TryComp(uid, out TargetedProjectileComponent? targeted) && targeted.Target == args.OtherEntity)
            return;
        // /Goobstation

        if (component.IgnoreShooter && (args.OtherEntity == component.Shooter || args.OtherEntity == component.Weapon))
        {
            args.Cancelled = true;
        }
    }

    // Goobstation - Crawling fix
    private void EmbeddablePreventCollision(EntityUid uid, EmbeddableProjectileComponent component, ref PreventCollideEvent args)
    {
        if (TryComp<RequireProjectileTargetComponent>(args.OtherEntity, out var requireTarget) && requireTarget.IgnoreThrow && requireTarget.Active)
            args.Cancelled = true;
    }

    public void SetShooter(EntityUid id, ProjectileComponent component, EntityUid shooterId)
    {
        if (component.Shooter == shooterId)
            return;

        component.Shooter = TerminatingOrDeleted(shooterId) ? null : shooterId; // Goobstation - set to null if deleted
        Dirty(id, component);
    }

    [Serializable, NetSerializable]
    private sealed partial class RemoveEmbeddedProjectileEvent : DoAfterEvent
    {
        public override DoAfterEvent Clone() => this;
    }
}

[Serializable, NetSerializable]
public sealed class ImpactEffectEvent : EntityEventArgs
{
    public string Prototype;
    public NetCoordinates Coordinates;

    public ImpactEffectEvent(string prototype, NetCoordinates coordinates)
    {
        Prototype = prototype;
        Coordinates = coordinates;
    }
}

/// <summary>
/// Raised when an entity is just about to be hit with a projectile but can reflect it
/// </summary>
[ByRefEvent]
public record struct ProjectileReflectAttemptEvent(EntityUid ProjUid, ProjectileComponent Component, bool Cancelled) : IInventoryRelayEvent
{
    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.WITHOUT_POCKET;
}

/// <summary>
/// Raised when a projectile hits an entity
/// </summary>
[ByRefEvent]
public record struct ProjectileHitEvent(DamageSpecifier Damage, EntityUid Target, EntityUid? Shooter = null, bool Handled = false);
