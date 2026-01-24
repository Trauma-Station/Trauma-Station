using Content.Shared.EntityEffects;
using Content.Shared.Projectiles;

namespace Content.Goobstation.Shared.Mimery;

public sealed class EntityEffectOnProjectileHitSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityEffectOnProjectileHitComponent, ProjectileHitEvent>(OnHit);
    }

    private void OnHit(Entity<EntityEffectOnProjectileHitComponent> ent, ref ProjectileHitEvent args)
    {
        _effects.ApplyEffects(args.Target, ent.Comp.Effects.ToArray(), user: args.Shooter);
    }
}
