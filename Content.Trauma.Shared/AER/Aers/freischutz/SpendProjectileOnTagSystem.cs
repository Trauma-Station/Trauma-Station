// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Content.Shared.Tag;
using Content.Shared.Projectiles;
using Content.Shared.Damage.Systems;

namespace Content.Trauma.Shared.AER;

/// <summary>
/// system for expending projectiles on hitting an item with a certain tag
/// </summary>
public sealed partial class SpendProjectileOnTagSystem : EntitySystem
{
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    //public static readonly ProtoId<TagPrototype> Wall = "Wall";

    [SubscribeLocalEvent]
    private void OnTagHit(Entity<SpendProjectileOnTagComponent> bullet, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != SharedProjectileSystem.ProjectileFixture || !args.OtherFixture.Hard)
            return;

        DoHit((bullet.Owner, bullet.Comp, args.OurBody), args.OtherEntity, args.OtherFixture);
    }

    public void DoHit(Entity<SpendProjectileOnTagComponent, PhysicsComponent> ent, EntityUid target, Fixture otherFixture)
    {
        var tag = ent.Comp1.Tag;

        if (!TryComp<TagComponent>(target, out var targetComp))
            return;

        if (!_tag.HasTag(targetComp, tag))
            return;

        if (!TryComp<ProjectileComponent>(ent.Owner, out var projectileComponent))
            return;

        projectileComponent.ProjectileSpent = true;
    }
}
