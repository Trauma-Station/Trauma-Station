// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Content.Trauma.Shared.Teleportation;
using Robust.Shared.Spawners;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Teleports user of an item with LinkedEntityContainer to linked item.
/// </summary>
public sealed partial class ToLinkedEntityTeleport : EntityEffectBase<ToLinkedEntityTeleport>
{
    /// <summary>
    /// Spawns one way portal.
    /// </summary>
    [DataField]
    public EntProtoId<PortalComponent>? SpawnPortal;
}

public sealed partial class ToLinkedEntityTeleportEffectSystem : EntityEffectSystem<HandsComponent, ToLinkedEntityTeleport>
{
    [Dependency] private LinkedEntitySystem _link = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private TeleportSystem _teleport = default!;

    protected override void Effect(Entity<HandsComponent> ent, ref EntityEffectEvent<ToLinkedEntityTeleport> args)
    {
        var handEnt = _hands.GetActiveItemOrSelf((ent.AsNullable()));

        if (!_link.GetLink(handEnt, out var linkedEnt) ||
            !linkedEnt.Value.IsValid())
            return;

        var oldPos = Transform(ent).Coordinates;
        var newPos = Transform(linkedEnt.Value).Coordinates;

        _teleport.Teleport(ent.Owner, newPos, user: args.User);

        if (args.Effect.SpawnPortal != default)
        {
            var portal = PredictedSpawnAtPosition(args.Effect.SpawnPortal, oldPos);
            _link.TryLink(portal, linkedEnt.Value, true);
        }
    }
}
