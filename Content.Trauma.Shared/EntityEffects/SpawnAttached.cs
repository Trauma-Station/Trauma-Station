// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Coordinates;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.EntitySpawning;
using Robust.Shared.Network;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Spawns entities attached to the target entity, instead of dropped next to it.
/// </summary>
public sealed partial class SpawnAttached : BaseSpawnEntityEntityEffect<SpawnAttached>;

public sealed partial class SpawnAttachedEntityEffectSystem : EntityEffectSystem<TransformComponent, SpawnAttached>
{
    [Dependency] private readonly INetManager _net = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<SpawnAttached> args)
    {
        var quantity = args.Effect.Number * (int) Math.Floor(args.Scale);
        var proto = args.Effect.Entity;
        var coords = entity.Owner.ToCoordinates();

        if (args.Effect.Predicted)
        {
            for (var i = 0; i < quantity; i++)
            {
                PredictedSpawnAttachedTo(proto, coords);
            }
        }
        else if (_net.IsServer)
        {
            for (var i = 0; i < quantity; i++)
            {
                SpawnAttachedTo(proto, coords);
            }
        }
    }
}
