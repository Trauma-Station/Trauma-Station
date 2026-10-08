// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.EntitySpawning;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Spawns entities at coordinates, and attempts to anchor them
/// </summary>
public sealed partial class SpawnAnchored : BaseSpawnEntityEntityEffect<SpawnAnchored>;

public sealed partial class SpawnAnchoredEntityEffectSystem : EntityEffectSystem<TransformComponent, SpawnAnchored>
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedTransformSystem _xform = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<SpawnAnchored> args)
    {
        var quantity = args.Effect.Number * (int) Math.Floor(args.Scale);
        var proto = args.Effect.Entity;
        var coords = entity.Comp.Coordinates;

        if (_net.IsClient && !(args.Effect.Predicted && args.Predicted))
            return;

        for (var i = 0; i < quantity; i++)
        {
            var spawned = PredictedSpawnAtPosition(proto, coords);
            var xform = Transform(spawned);
            if (!xform.Anchored)
                _xform.AnchorEntity((spawned, xform));
        }
    }
}
