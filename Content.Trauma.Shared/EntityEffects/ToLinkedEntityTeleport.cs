// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Content.Trauma.Shared.LinkedEntityContainer;
using Content.Trauma.Shared.Teleportation;
using Robust.Shared.Spawners;

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
        var handEntity = _hands.GetActiveItemOrSelf((ent.Owner, ent.Comp));

        if (!TryComp<LinkedEntityContainerComponent>(handEntity, out var destination) || !destination.Uid.IsValid())
            return;
        var targetEnt = destination.Uid;

        var oldPos = Transform(ent).Coordinates;
        var newPos = Transform(targetEnt).Coordinates;

        _teleport.Teleport(ent.Owner, newPos, user: args.User);

        if (args.Effect.SpawnPortal != default)
        {
            var portal = PredictedSpawnAtPosition(args.Effect.SpawnPortal, oldPos);
            _link.OneWayLink(portal, targetEnt, true);

            var linkedComp = EnsureComp<LinkedEntityContainerComponent>(portal).Uid = destination.Uid;
            Ditry(portal, linkedComp);
        }
    }
}
