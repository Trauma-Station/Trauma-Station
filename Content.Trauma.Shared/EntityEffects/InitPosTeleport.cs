// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Random.Helpers;
using Content.Trauma.Shared.InitPos;
using Content.Trauma.Shared.Teleportation.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Teleportation;

/// <summary>
/// Teleports user of an item with InitPosComponent to items initial position.
/// </summary>
public sealed partial class InitPosTeleport : EntityEffectBase<InitPosTeleport>
{
    /// <summary>
    /// Spawns one way portal.
    /// </summary>
    [DataField]
    public EntProtoId? SpawnPortal;
}

public sealed partial class InitPosTeleportEffectSystem : EntityEffectSystem<HandsComponent, InitPosTeleport>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private TeleportSystem _teleport = default!;
    [Dependency] private RandomTeleportSystem _randomTeleport = default!;

    protected override void Effect(Entity<HandsComponent> ent, ref EntityEffectEvent<InitPosTeleport> args)
    {
        var handEntity = _hands.GetActiveItemOrSelf((ent.Owner, ent.Comp));

        if (!TryComp<InitPosComponent>(handEntity, out var destination))
            return;

        var oldPos = Transform(ent).Coordinates;

        var newPos = Transform(ent).Coordinates.WithPosition(destination.Coordinates.Position);

        Log.Info($"{ent.Owner}");
        Log.Info($"{args.User}");
        Log.Info($"{oldPos}");
        Log.Info($"{newPos}");
        Log.Info($"{destination.Coordinates}");
        _teleport.Teleport(ent.Owner, newPos, user: args.User); // it keeps breaking and i don't know why

        if (args.Effect.SpawnPortal != default)
        {
            var portal = PredictedSpawnAtPosition(args.Effect.SpawnPortal, oldPos);

            if (!TryComp<OneWayPortalComponent>(portal, out var comp))
            {
                PredictedDel(portal);
                return;
            }

            comp.Destination = destination.Coordinates;
            Dirty(portal, comp);
        }
    }
}
