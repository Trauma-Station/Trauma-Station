using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.EntitySpawning;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.EntityEffects.Effects;

/// <summary>
/// Spawns a random number of entities at the target.
/// <see cref="Number"/> is the inclusive maximum number of entities to spawn, the minimum is 1.
/// </summary>
public sealed partial class SpawnRandomEntities : BaseSpawnEntityEntityEffect<SpawnRandomEntities>;

public sealed class SpawnRandomEntitiesEffectSystem : EntityEffectSystem<TransformComponent, SpawnRandomEntities>
{
    [Dependency] private readonly IGameTiming _timing = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<SpawnRandomEntities> args)
    {
        var seed = SharedRandomExtensions.HashCodeCombine((int) _timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);
        var quantity = rand.Next(1, args.Effect.Number + 1);

        var proto = args.Effect.Entity;
        for (var i = 0; i < quantity; i++)
        {
            PredictedSpawnNextToOrDrop(proto, ent);
        }
    }
}
