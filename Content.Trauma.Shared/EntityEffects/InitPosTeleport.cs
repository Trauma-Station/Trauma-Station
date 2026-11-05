// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Trauma.Shared.InitPos;
using Content.Trauma.Shared.Teleportation;

namespace Content.Trauma.Shared.EntityEffects;

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

        // otherwise will trow try parent from discendant exception
        var newPos = Transform(ent).Coordinates.WithPosition(destination.Coordinates.Position);
        //newPos.MapID = destination.Map;
        // set transform.MapID maybe?? if so then ill have to reshittify portal system
        // at this poit it'll be better to just spawn dummy ent at the creation point...

        _teleport.Teleport(ent.Owner, newPos, user: args.User);

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
