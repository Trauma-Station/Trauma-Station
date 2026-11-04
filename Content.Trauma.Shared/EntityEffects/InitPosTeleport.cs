// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Trauma.Shared.InitPos;
using Content.Trauma.Shared.Teleportation.Systems;

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
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private TeleportSystem _teleport = default!;

    protected override void Effect(Entity<HandsComponent> ent, ref EntityEffectEvent<InitPosTeleport> args)
    {
        var handEntity = _hands.GetActiveItemOrSelf((ent.Owner, ent.Comp));

        if (!TryComp<InitPosComponent>(handEntity, out var destination))
            return;

        var oldPos = Transform(ent).Coordinates;

        _teleport.Teleport(ent.Owner, destination.Coordinates, user: args.User);

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
