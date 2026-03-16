using System.Numerics;
using Content.Shared._DV.CosmicCult.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared._DV.CosmicCult.Abilities;

public sealed class CosmicNovaSystem : EntitySystem
{
    [Dependency] private readonly SharedCosmicCultSystem _cult = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedInteractionSystem _interact = default!;

    private static readonly EntProtoId Projectile = "ProjectileCosmicNova";

    private HashSet<Entity<MobStateComponent>> _mobs = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultComponent, EventCosmicNova>(OnCosmicNova);
        SubscribeLocalEvent<CosmicAstralNovaComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<CosmicAstralNovaComponent, ProjectileHitEvent>(OnNovaCollide);
    }

    /// <summary>
    /// This is the basic spell projectile code but updated to use non-obsolete functions, all so i can change the default projectile speed. Fuck.
    /// </summary>
    private void OnCosmicNova(Entity<CosmicCultComponent> ent, ref EventCosmicNova args)
    {
        var startPos = _transform.GetMapCoordinates(args.Performer);
        var targetPos = _transform.ToMapCoordinates(args.Target);
        var userVelocity = Vector2.Zero; // Actually using this makes it near impossible to aim while moving, and possible to hurl it way too fast

        var delta = targetPos.Position - startPos.Position;
        if (delta.EqualsApprox(Vector2.Zero))
            delta = new(.01f, 0);

        args.Handled = true;
        var projectile = PredictedSpawnAtPosition(Projectile, Transform(ent).Coordinates);
        _gun.ShootProjectile(projectile, delta, userVelocity, args.Performer, args.Performer, 7f);
        _audio.PlayPredicted(ent.Comp.NovaCastSFX, ent, ent, AudioParams.Default.WithVariation(0.1f));
        _cult.MalignEcho(ent);
    }

    /// <summary>
    /// If the projectile collides with another cultist, it passes right through them
    /// </summary>
    private void OnPreventCollide(Entity<CosmicAstralNovaComponent> ent, ref PreventCollideEvent args)
    {
        if (_entityWhitelist.IsValid(ent.Comp.AreaBlacklist, args.OtherEntity))
            args.Cancelled = true;
    }

    private void OnNovaCollide(Entity<CosmicAstralNovaComponent> ent, ref ProjectileHitEvent args)
    {
        _mobs.Clear();
        _lookup.GetEntitiesInRange(Transform(ent).Coordinates, ent.Comp.AreaRange, _mobs);
        _mobs.RemoveWhere(target =>
        {
            if (_entityWhitelist.IsValid(ent.Comp.AreaBlacklist, target)) return true;

            var evt = new CosmicAbilityAttemptEvent(target);
            RaiseLocalEvent(ref evt);
            if (evt.Cancelled) return true;

            return !_interact.InRangeUnobstructed(
                (ent.Owner, Transform(ent)),
                (target.Owner, Transform(target)),
                range: ent.Comp.AreaRange,
                collisionMask: CollisionGroup.Impassable);
        });
        foreach (var mob in _mobs)
        {
            _stun.TryAddParalyzeDuration(mob, ent.Comp.StunDuration);
            _damageable.TryChangeDamage(mob.Owner, ent.Comp.AreaDamage);
        }
    }
}
