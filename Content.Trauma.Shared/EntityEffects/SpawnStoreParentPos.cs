// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.EntitySpawning;
using Content.Trauma.Shared.InitPos;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Spawns an ent with InitPos component. Ent with this comp should not be spawned with other events ever.
/// </summary>
public sealed partial class SpawnStoreParentPos : BaseSpawnEntityEntityEffect<SpawnStoreParentPos>;

public sealed partial class SpawnStoreParentPosEffectSystem : EntityEffectSystem<TransformComponent, SpawnStoreParentPos>
{
    [Dependency] private InitPosSystem _initPos = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<SpawnStoreParentPos> args)
    {
        var quantity = args.Effect.Number * (int)Math.Floor(args.Scale);
        var proto = args.Effect.Entity;

        for (var i = 0; i < quantity; i++)
        {
            _initPos.SpawnWithParentPos(args.Effect.Entity, entity);
        }
    }
}