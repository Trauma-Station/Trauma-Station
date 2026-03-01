// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Trauma.Common.Bulletholes;
using Content.Trauma.Shared.Weapons.Ranged;
using Content.Trauma.Shared.Weapons.Ranged.Ammo;
using Robust.Shared.Random;

namespace Content.Trauma.Server.Bulletholes;

/// <summary>
/// Handles giving bullet holes a position and sending it to the client
/// </summary>
public sealed class BulletHoleSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BulletHoleComponent, GotHitByProjectileEvent>(OnHit);
    }

    private void OnHit(Entity<BulletHoleComponent> ent, ref GotHitByProjectileEvent args)
    {
        if (!HasComp<BulletHoleGeneratorComponent>(args.Projectile))
            return;

        if (ent.Comp.HolePositions.Count >= BulletHoleComponent.MaxHoles)
            return;

        var offset = new Vector2(
            _random.NextFloat() * 0.8f + 0.1f,
            _random.NextFloat() * 0.8f + 0.1f);

        ent.Comp.HolePositions.Add(offset);
        Dirty(ent);
    }
}
