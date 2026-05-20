// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.Tag;
using Content.Trauma.Common.Heretic;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Holosign;

/// <summary>
/// Trauma - helper functions for Holosign rework
/// </summary>
public sealed partial class HolosignSystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IMapManager _mapMan = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private SharedChargesSystem _charges = default!;

    public static readonly ProtoId<TagPrototype> HolosignTag = "Holosign";

    private const int BlockMask = (int) (
        CollisionGroup.Impassable |
        CollisionGroup.HighImpassable);

    private EntityQuery<PhysicsComponent> _physicsQuery;

    private void InitializeTrauma()
    {
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
    }

    private EntityCoordinates? CheckCoords(Entity<HolosignProjectorComponent> ent, ref BeforeRangedInteractEvent args)
    {
        var ev = new BeforeHolosignUsedEvent(args.User, args.ClickLocation);
        RaiseLocalEvent(ent, ref ev);
        if (ev.Cancelled || !ev.Handled && !args.CanReach)
            return null;

        // places the holographic sign at the click location, snapped to grid.
        var coords = args.ClickLocation.SnapToGrid(EntityManager);
        var mapCoords = _transform.ToMapCoordinates(coords);
        var look = _mapMan.TryFindGridAt(mapCoords, out var grid, out var gridComp)
            ? _map.GetAnchoredEntities((grid, gridComp), mapCoords)
            : _lookup.GetEntitiesInRange(mapCoords, 0.1f);
        foreach (var entity in look)
        {
            if (!_physicsQuery.TryComp(entity, out var physics))
                continue;

            if (_tag.HasTag(entity, HolosignTag))
                return null; // no stacking holosigns

            if ((physics.CollisionLayer & BlockMask) != 0) // overlapping with something that blocks the field
                return null;
        }

        EntityUid? user = TryComp(ent, out LimitedChargesComponent? charges) ? null : args.User; // Don't show popups if it has limited charges (user is null = no popup)
        if (!_powerCell.TryUseCharge(ent.Owner, ent.Comp.ChargeUse, user: user) && !_charges.TryUseCharge((ent, charges))) // if no battery or no charge, doesn't work
            return null;

        return coords;
    }
}
