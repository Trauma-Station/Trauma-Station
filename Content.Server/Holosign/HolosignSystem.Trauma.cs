using Content.Shared.Coordinates.Helpers;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Server.Holosign;

/// <summary>
/// Trauma - helper function for Holosign rework
/// </summary>
public sealed partial class HolosignSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;

    private const int BlockMask = (int) (
        CollisionGroup.Impassable |
        CollisionGroup.LowImpassable |
        CollisionGroup.MidImpassable |
        CollisionGroup.HighImpassable);

    private EntityQuery<PhysicsComponent> _physicsQuery;

    private void InitializeTrauma()
    {
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
    }

    private EntityCoordinates? CheckCoords(Entity<HolosignProjectorComponent> ent, BeforeRangedInteractEvent args)
    {
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

            if (_tag.HasTag(entity, ent.Comp.HolosignTag))
                return null; // no stacking holosigns

            if ((physics.CollisionLayer & BlockMask) != 0) // overlapping with something that blocks the field
                return null;
        }

        return _powerCell.TryUseCharge(ent.Owner, ent.Comp.ChargeUse, user: args.User) // if no battery or no charge, doesn't work
            ? coords
            : null;
    }
}
