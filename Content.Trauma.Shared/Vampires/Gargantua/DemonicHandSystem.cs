using Content.Lavaland.Common.Weapons.Ranged;
using Content.Shared.CombatMode;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Robust.Shared.Map;

namespace Content.Trauma.Shared.Vampires.Gargantua;

public sealed class DemonicHandSystem : EntitySystem
{
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throw = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DemonicHandComponent, ProjectileHitEvent>(OnHit);
    }

    private void OnHit(Entity<DemonicHandComponent> ent, ref ProjectileHitEvent args)
    {
        if (args.Shooter is not { } shooter)
            return;

        var xform = Transform(shooter);
        var targetXform = Transform(args.Target);

        // Technically, if you switch fast enough to combat mode during the shooting,
        // you will be able to change behaviour. It is not really a bug, therefore
        // it will be kept because it reduces the amount of coding I do (I cbf) and it's a nice mechanic.
        if (_combat.IsInCombatMode(shooter))
        {
            _throw.TryThrow(args.Target, xform.Coordinates, 30f, shooter);
            return;
        }

        //TODO: Test this fr
        var mapCoordsShooter = _transform.GetMapCoordinates(xform);
        var mapCoordsTarget = _transform.GetMapCoordinates(targetXform);
        var dir = (mapCoordsShooter.Position - mapCoordsTarget.Position).Normalized();

        _throw.TryThrow(args.Target, dir, 30f, shooter);
    }
}
