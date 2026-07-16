// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Projectiles;

public sealed partial class ProjectileImmunitySystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProjectileImmunityComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<ProjectileImmunityComponent, ProjectileReflectAttemptEvent>(OnProjectileReflect);
        SubscribeLocalEvent<ProjectileImmunityComponent, HitScanReflectAttemptEvent>(OnHitscanReflect);
    }

    private void OnPreventCollide(Entity<ProjectileImmunityComponent> ent, ref PreventCollideEvent args)
    {
        if (!HasComp<ProjectileComponent>(args.OtherEntity))
            return;

        if (!args.OtherFixture.Hard)
            return;

        args.Cancelled = true;

        if (_timing.IsFirstTimePredicted)
            TrySpawnDodgeEffect(ent, args.OtherEntity);
    }

    private void OnProjectileReflect(Entity<ProjectileImmunityComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        args.Cancelled = true;

        if (_timing.IsFirstTimePredicted)
            TrySpawnDodgeEffect(ent, args.ProjUid);
    }

    private void OnHitscanReflect(Entity<ProjectileImmunityComponent> ent, ref HitScanReflectAttemptEvent args)
    {
        if (args.Reflected)
            return;

        args.Reflected = true;

        if (ent.Comp.DodgeEffect is { } effect)
            PredictedSpawnAttachedTo(effect, new EntityCoordinates(ent, Vector2.Zero));
    }

    private void TrySpawnDodgeEffect(Entity<ProjectileImmunityComponent> ent, EntityUid projectile)
    {
        if (ent.Comp.DodgeEffect is not { } effect)
            return;

        if (TerminatingOrDeleted(projectile))
            return;

        if (!ent.Comp.DodgedEntities.Add(projectile))
            return;

        PredictedSpawnAtPosition(effect, new EntityCoordinates(ent, Vector2.Zero));
    }
}
