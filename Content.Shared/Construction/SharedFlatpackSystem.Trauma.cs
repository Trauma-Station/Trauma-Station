using Content.Shared.Construction.Components;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Construction;

/// <summary>
/// Trauma - better flatpack logic
/// </summary>
public abstract partial class SharedFlatpackSystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    public bool IsTileOccupied(Entity<FlatpackComponent> ent, EntityCoordinates coords)
        // unreachable
        => ent.Comp.Entity is {} id &&
            // if the machine has no fixtures it by definition can't occupy a tile, so it will return false
            PrototypeManager.Index(id).TryGetComponent<FixturesComponent>(out var fixtures, Factory) &&
            // unreachable
            _turf.GetTileRef(coords) is {} tile &&
            // checks that the machine isnt blocked by anything
            _turf.IsTileBlocked(tile, GetMask(fixtures));

    private CollisionGroup GetMask(FixturesComponent fixtures)
    {
        var (_, mask) = SharedPhysicsSystem.GetHardCollision(fixtures);
        return (CollisionGroup) mask;
    }
}
