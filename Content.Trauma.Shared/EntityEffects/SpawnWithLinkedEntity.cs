// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects.Effects.EntitySpawning;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.LinkedEntityContainer;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Spawns an ent with LinkedEntityContainerComponent component. Ent with this comp should not be spawned with other events ever.
/// </summary>
public sealed partial class SpawnWithLinkedEntity : BaseSpawnEntityEntityEffect<SpawnWithLinkedEntity>;

public sealed partial class SpawnWithLinkedEntityEffectSystem : EntityEffectSystem<TransformComponent, SpawnWithLinkedEntity>
{
    [Dependency] private LinkedEntityContainerSystem _linked = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<SpawnWithLinkedEntity> args)
    {
        var quantity = args.Effect.Number * (int)Math.Floor(args.Scale);
        var proto = args.Effect.Entity;

        for (var i = 0; i < quantity; i++)
        {
            _linked.SpawnWithLinkedEntity(args.Effect.Entity, entity);
        }
    }
}
