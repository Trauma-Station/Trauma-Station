// <Trauma>
using Content.Goobstation.Common.CCVar;
using Content.Goobstation.Common.Projectiles;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Components;
// </Trauma>
using Content.Shared.Damage.Components;
using Content.Shared.Projectiles;
using Content.Shared.Standing;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Damage.Systems;

public sealed partial class RequireProjectileTargetSystem : EntitySystem
{
    // <Trauma>
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    // </Trauma>
    [Dependency] private SharedContainerSystem _container = default!;

    private float _crawlHitzoneSquared; // Goob

    public override void Initialize()
    {
        _cfg.OnValueChanged(GoobCVars.CrawlHitzoneSize, x => _crawlHitzoneSquared = x * x, true); // Goob - squared now as a micro-optimisation
        SubscribeLocalEvent<RequireProjectileTargetComponent, PreventCollideEvent>(PreventCollide);
        SubscribeLocalEvent<RequireProjectileTargetComponent, StoodEvent>(StandingBulletHit);
        SubscribeLocalEvent<RequireProjectileTargetComponent, DownedEvent>(LayingBulletPass);
    }

    private void PreventCollide(Entity<RequireProjectileTargetComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled)
            return;

        if (!ent.Comp.Active)
            return;

        var other = args.OtherEntity;
        // Goob edit start
        if (TryComp(other, out TargetedProjectileComponent? targeted))
        {
            if (GetEntity(targeted.Target) is not {} target || target == ent.Owner)
                return;

            var ev = new ShouldTargetedProjectileCollideEvent(target);
            RaiseLocalEvent(ent, ev);
            if (ev.Handled)
                return;
        }

        if (TryComp(other, out ProjectileComponent? projectile))
        {
            // Goob edit end

            // Prevents shooting out of while inside of crates
            var shooter = projectile.Shooter;
            if (!shooter.HasValue)
                return;

            if (TryComp(ent, out PhysicsComponent? physics) && physics.LinearVelocity.Length() > 2.5f) // Goobstation
                return;

            // ProjectileGrenades delete the entity that's shooting the projectile,
            // so it's impossible to check if the entity is in a container
            if (TerminatingOrDeleted(shooter.Value))
                return;

            // <Goob>
            if ((_transform.GetMapCoordinates(ent).Position - projectile.TargetCoordinates).LengthSquared() <= _crawlHitzoneSquared)
                return;
            // </Goob>

            if (!_container.IsEntityOrParentInContainer(shooter.Value))
                args.Cancelled = true;
        }
    }

    private void SetActive(Entity<RequireProjectileTargetComponent> ent, bool value)
    {
        if (ent.Comp.Active == value)
            return;

        ent.Comp.Active = value;
        Dirty(ent);
    }

    private void StandingBulletHit(Entity<RequireProjectileTargetComponent> ent, ref StoodEvent args)
    {
        SetActive(ent, false);
    }

    private void LayingBulletPass(Entity<RequireProjectileTargetComponent> ent, ref DownedEvent args)
    {
        SetActive(ent, true);
    }
}
