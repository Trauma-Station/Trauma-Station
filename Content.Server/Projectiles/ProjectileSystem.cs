using Content.Goobstation.Maths.FixedPoint;
using Content.Server.Destructible;
using Content.Shared.Projectiles;

namespace Content.Server.Projectiles;

public sealed class ProjectileSystem : SharedProjectileSystem
{
    [Dependency] private readonly DestructibleSystem _destructible = default!;

    protected override FixedPoint2 GetDestructionDamage(EntityUid target)
    {
        return _destructible.DestroyedAt(target);
    }
}
