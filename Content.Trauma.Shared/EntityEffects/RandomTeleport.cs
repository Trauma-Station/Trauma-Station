// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Destructible.Thresholds;
using Content.Shared.EntityEffects;
using Content.Shared.Teleportation.Systems;
using Content.Trauma.Shared.LinkedEntityContainer;
using Content.Trauma.Shared.Teleportation.Systems;
using Robust.Shared.Spawners;

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
    public EntProtoId<PortalComponent>? SpawnPortal;

    /// <summary>
    /// Prototyte to spawn for portal destination.
    /// </summary>
    [DataField]
    public EntProtoId PortalDestinationEnt = "BluespaceRiftPortalDestination";
}

public sealed partial class RandomTeleportEffectSystem : EntityEffectSystem<TransformComponent, RandomTeleport>
{
    [Dependency] private LinkedEntitySystem _link = default!;
    [Dependency] private RandomTeleportSystem _teleport = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<RandomTeleport> args)
    {
        var oldPos = ent.Comp.Coordinates;

        var newPos = _teleport.RandomTeleport(ent, args.Effect.Radius, args.Effect.TeleportAttempts, user: args.User);

        if (args.Effect.SpawnPortal != default)
        {
            var portal = PredictedSpawnAtPosition(args.Effect.SpawnPortal, oldPos);

            var targetEnt = PredictedSpawnAtPosition(args.Effect.PortalDestinationEnt, newPos);

            _link.OneWayLink(portal, targetEnt, true);

            var linkedComp = EnsureComp<LinkedEntityContainerComponent>(portal);
            linkedComp.Uid = targetEnt;
            Dirty(portal, linkedComp);
        }
    }
}
