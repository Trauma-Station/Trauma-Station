using Content.Shared._Starfall.Particles;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Content.Shared.Trigger;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Prototypes;

namespace Content.Client._Starfall.Particles;

/// <summary>
/// Client-side system that listens for events and spawns particles based on <see cref="ParticleOnEventComponent"/>
/// </summary>
public sealed class ParticleOnEventSystem : EntitySystem
{
    [Dependency] private readonly ParticleSystem _particles = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    // Track emitters spawned by OnThrown so we can stop them when the entity lands
    private readonly Dictionary<EntityUid, ActiveEmitter> _thrownEmitters = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParticleOnUseComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<ParticleOnUseInWorldComponent, AfterInteractEvent>(OnUseInWorld);
        SubscribeLocalEvent<ParticleOnMeleeAttackComponent, MeleeHitEvent>(OnMeleeAttack);
        SubscribeLocalEvent<ParticleOnMeleeAttackOtherComponent, MeleeHitEvent>(OnMeleeAttackOther);
        SubscribeLocalEvent<ParticleOnMeleeHitComponent, AttackedEvent>(OnMeleeHit);
        SubscribeLocalEvent<ParticleOnThrownComponent, ThrownEvent>(OnThrown);
        SubscribeLocalEvent<ParticleOnThrownComponent, LandEvent>(OnThrownLanded);
        SubscribeLocalEvent<ParticleOnThrownComponent, ComponentShutdown>(OnThrownShutdown);
        SubscribeLocalEvent<ParticleOnLandedComponent, LandEvent>(OnLanded);
        SubscribeLocalEvent<ParticleOnPrimedComponent, ActiveTimerTriggerEvent>(OnPrimed);
        SubscribeLocalEvent<ParticleOnGunShotComponent, AmmoShotEvent>(OnGunShot);
        SubscribeLocalEvent<ParticleOnGunShotProjectileComponent, AmmoShotEvent>(OnGunShotProjectile);
        SubscribeLocalEvent<ParticleOnProjectileHitComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<ParticleOnProjectileHitOtherComponent, ProjectileHitEvent>(OnProjectileHitOther);

    }

    private void OnUse(Entity<ParticleOnUseComponent> ent, ref UseInHandEvent args)
    {
        if (!args.Handled)
            Spawn(ent.Comp, ent.Owner);
    }

    private void OnUseInWorld(Entity<ParticleOnUseInWorldComponent> ent, ref AfterInteractEvent args)
    {
        if (args.CanReach)
            Spawn(ent.Comp, ent.Owner);
    }

    private void OnMeleeAttack(Entity<ParticleOnMeleeAttackComponent> ent, ref MeleeHitEvent args)
        => Spawn(ent.Comp, ent.Owner);

    private void OnMeleeAttackOther(Entity<ParticleOnMeleeAttackOtherComponent> ent, ref MeleeHitEvent args)
    {
        foreach (var victim in args.HitEntities)
        {
            Spawn(ent.Comp, victim);
        }
    }

    private void OnMeleeHit(Entity<ParticleOnMeleeHitComponent> ent, ref AttackedEvent args)
        => Spawn(ent.Comp, ent.Owner);

    private void OnThrown(Entity<ParticleOnThrownComponent> ent, ref ThrownEvent args)
    {
        // Allow infinite-duration effects the emitter is stopped when the entity lands.
        var emitter = _particles.CreateParticle(ent.Comp.Effect, ent.Owner, ent.Comp.ColorOverride);
        if (emitter != null)
            _thrownEmitters[ent.Owner] = emitter;
    }

    private void OnThrownLanded(Entity<ParticleOnThrownComponent> ent, ref LandEvent args)
    {
        if (_thrownEmitters.Remove(ent.Owner, out var emitter))
            _particles.RemoveParticle(emitter);
    }

    private void OnThrownShutdown(Entity<ParticleOnThrownComponent> ent, ref ComponentShutdown args)
    {
        if (_thrownEmitters.Remove(ent.Owner, out var emitter))
            _particles.RemoveParticle(emitter);
    }

    private void OnLanded(Entity<ParticleOnLandedComponent> ent, ref LandEvent args)
        => Spawn(ent.Comp, ent.Owner);

    private void OnPrimed(Entity<ParticleOnPrimedComponent> ent, ref ActiveTimerTriggerEvent args)
        => Spawn(ent.Comp, ent.Owner);

    private void OnGunShot(Entity<ParticleOnGunShotComponent> ent, ref AmmoShotEvent args)
        => Spawn(ent.Comp, ent.Owner);

    private void OnGunShotProjectile(Entity<ParticleOnGunShotProjectileComponent> ent, ref AmmoShotEvent args)
    {
        // Infinite-duration allowed: the emitter is cleaned up when the projectile is destroyed.
        foreach (var projectile in args.FiredProjectiles)
        {
            _particles.CreateParticle(ent.Comp.Effect, projectile, ent.Comp.ColorOverride);
        }
    }

    private void OnProjectileHit(Entity<ParticleOnProjectileHitComponent> ent, ref ProjectileHitEvent args)
        => Spawn(ent.Comp, ent.Owner);

    private void OnProjectileHitOther(Entity<ParticleOnProjectileHitOtherComponent> ent, ref ProjectileHitEvent args)
        => Spawn(ent.Comp, args.Target);

    private void Spawn(ParticleOnEventBase comp, EntityUid target)
    {
        if (!_proto.TryIndex(comp.Effect, out var proto))
        {
            Log.Error($"ParticleOnEvent references unknown effect '{comp.Effect}'");
            return;
        }

        if (proto.Duration == TimeSpan.Zero)
        {
            Log.Warning($"ParticleOnEvent tried to spawn infinite-duration effect '{comp.Effect}'. " +
                        "Infinite effects cannot be used with ParticleOnEvent, they never stop emitting.");
            return;
        }

        _particles.CreateParticle(comp.Effect, target, comp.ColorOverride);
    }

}

