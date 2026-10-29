// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Coordinates.Helpers;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.EntitySpawning;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Effect that spawns ent snapped to grid.
/// </summary>
public sealed partial class SpawnSnappedToGrid : BaseSpawnEntityEntityEffect<SpawnSnappedToGrid>;

public sealed partial class SpawnSnappedToGridEntityEffectSystem : EntityEffectSystem<TransformComponent, SpawnSnappedToGrid>
{
    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<SpawnSnappedToGrid> args)
    {
        var pos = ent.Comp.Coordinates.SnapToGrid(EntityManager);
        PredictedSpawnAtPosition(args.Effect.Entity, pos);
    }
}
