// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Content.Shared.Projectiles;
using Content.Shared.Wall;

namespace Content.Trauma.Shared.AER;

/// <summary>
/// system for expending projectiles on hitting an item with a certain tag
/// </summary>
public sealed partial class AntiWallBangSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnTagHit(Entity<AntiWallBangComponent> bullet, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != SharedProjectileSystem.ProjectileFixture || !args.OtherFixture.Hard)
            return;

        DoHit((bullet.Owner, bullet.Comp, args.OurBody), args.OtherEntity, args.OtherFixture);
    }

    public void DoHit(Entity<AntiWallBangComponent, PhysicsComponent> ent, EntityUid target, Fixture otherFixture)
    {
        if (!TryComp<WallComponent>(target, out var targetComp))
            return;

        if (!TryComp<ProjectileComponent>(ent.Owner, out var projectileComponent))
            return;

        projectileComponent.ProjectileSpent = true;
    }
}
