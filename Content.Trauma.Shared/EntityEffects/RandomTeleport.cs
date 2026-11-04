// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Teleportation;
using Content.Trauma.Shared.Teleportation.Systems;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects;

public sealed partial class RandomTeleport : EntityEffectBase<RandomTeleport>
{
    /// <summary>
    /// Up to how far to teleport the user in tiles.
    /// </summary>
    [DataField]
    public MinMax Radius = new MinMax(5, 20);

    /// <summary>
    /// How many times to try to pick the destination. Larger number means the teleport is more likely to be safe.
    /// </summary>
    [DataField]
    public int TeleportAttempts = 10;

    /// <summary>
    /// Spawns one way portal.
    /// </summary>
    [DataField]
    public EntProtoId? SpawnPortal;
}

public sealed partial class RandomTeleportEffectSystem : EntityEffectSystem<TransformComponent, RandomTeleport>
{
    [Dependency] private RandomTeleportSystem _teleport = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<RandomTeleport> args)
    {
        var oldPos = ent.Comp.Coordinates;

        var newPos = _teleport.RandomTeleport(ent, args.Effect.Radius, args.Effect.TeleportAttempts, user: args.User);

        if (args.Effect.SpawnPortal != default)
        {
            var portal = PredictedSpawnAtPosition(args.Effect.SpawnPortal, oldPos);

            if (!TryComp<OneWayPortalComponent>(portal, out var comp))
            {
                PredictedDel(portal);
                return;
            }
            comp.Destination = newPos;
            Dirty(portal, comp);
        }
    }
}
