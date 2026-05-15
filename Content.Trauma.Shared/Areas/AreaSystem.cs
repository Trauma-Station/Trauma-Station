// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Roles;
using Robust.Shared.Map;

namespace Content.Trauma.Shared.Areas;

/// <summary>
/// Tracks area prototypes and provides API for using them.
/// </summary>
public sealed partial class AreaSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private MapAreaSystem _mapArea = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private EntityQuery<DepartmentAreaComponent> _deptQuery = default!;

    /// <summary>
    /// List of every area prototype in the game.
    /// </summary>
    [ViewVariables]
    public List<EntProtoId> AllAreas = new();

    /// <summary>
    /// Dictionary of departments to area prototypes that belong to it.
    /// </summary>
    [ViewVariables]
    public Dictionary<ProtoId<DepartmentPrototype>, List<EntProtoId>> DepartmentAreas = new();

    private const float Range = 0.25f;
    private const LookupFlags Flags = LookupFlags.Static;

    private HashSet<Entity<AreaComponent>> _areas = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AreaComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        LoadPrototypes();
    }

    private void OnAnchorStateChanged(Entity<AreaComponent> ent, ref AnchorStateChangedEvent args)
    {
        // delete areas that get unanchored by explosions or other more cursed things
        if (!args.Anchored)
            PredictedQueueDel(ent);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<EntityPrototype>())
            return;

        LoadPrototypes();
    }

    private void LoadPrototypes()
    {
        AllAreas.Clear();
        DepartmentAreas.Clear();
        var name = Factory.GetComponentName<AreaComponent>();
        var dept = Factory.GetComponentName<DepartmentAreaComponent>();
        foreach (var proto in _proto.EnumeratePrototypes<EntityPrototype>())
        {
            // TODO: proto.HasComp(name) after engine update
            if (!proto.Components.ContainsKey(name))
                continue;

            var id = proto.ID;
            AllAreas.Add(id);
            // TODO: proto.TryComp(name, Factory) after engine update
            if (!proto.TryGetComponent<DepartmentAreaComponent>(dept, out var comp))
                continue;

            var deptId = comp.Department;
            if (!DepartmentAreas.TryGetValue(deptId, out var list))
                DepartmentAreas[deptId] = list = [];
            list.Add(id);
        }
    }

    #region Public API

    /// <summary>
    /// Get the area a given mob is in.
    /// </summary>
    public EntityUid? GetArea(EntityUid target)
        => GetArea(Transform(target).Coordinates);

    /// <summary>
    /// Get the area at a given position by finding its grid first.
    /// </summary>
    public EntityUid? GetArea(EntityCoordinates coords)
        => _transform.GetGrid(coords) is {} grid
            ? GetArea(grid, coords)
            : null;

    /// <summary>
    /// Get the area at a given position on a grid.
    /// </summary>
    public EntityUid? GetArea(EntityUid grid, EntityCoordinates coords)
    {
        var pos = coords.Position;
        if (coords.EntityId != grid)
        {
            // relative to some random entity, have to go from world to grid-local first
            var matrix = _transform.GetInvWorldMatrix(grid);
            var worldPos = _transform.ToWorldPosition(coords);
            pos = Vector2.Transform(worldPos, matrix);
        }

        return _mapArea.GetArea(grid, pos);
    }

    /// <summary>
    /// Get the department an area belongs to, or null if it lacks <see cref="DepartmentAreaComponent"/>.
    /// </summary>
    public ProtoId<DepartmentPrototype>? GetAreaDepartment(EntityUid area)
        => _deptQuery.CompOrNull(area)?.Department;

    /// <summary>
    /// Gets the entity prototype of an area, or null if it lacks <see cref="EntityPrototype"/>.
    /// </summary>
    public EntProtoId? GetAreaPrototype(EntityUid area)
    {
        return Prototype(area)?.ID;
    }

    /// <summary>
    /// Add any areas not blocked by anything on a given map to a list, matching a predicate.
    /// </summary>
    public void AddOpenAreas(MapId map, List<Entity<TransformComponent>> areas, Predicate<Entity<TransformComponent>> pred)
    {
        AddOpenAreas<AreaComponent>(map, areas, pred);
    }

    /// <summary>
    /// Add areas not blocked by anything on a given map to a list, matching a predicate.
    /// Uses a generic component type param to narrow down the query, use a marker component for it to be faster.
    /// </summary>
    public void AddOpenAreas<T>(MapId map, List<Entity<TransformComponent>> areas, Predicate<Entity<TransformComponent>> pred) where T: IComponent
    {
        // TODO: open areas cache...
        var query = EntityQueryEnumerator<T, TransformComponent>();
        var mask = CollisionGroup.MobMask;
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapID != map)
                continue;

            var coords = xform.Coordinates;
            if (_turf.GetTileRef(coords) is not {} tile || _turf.IsTileBlocked(tile, mask))
                continue;

            var ent = new Entity<TransformComponent>(uid, xform);
            if (pred(ent))
                areas.Add(ent);
        }
    }

    /// <summary>
    /// Raises a by-ref event on the area a given mob is in.
    /// </summary>
    public void RaiseAreaEvent<T>(EntityUid target, ref T ev) where T: notnull
    {
        if (GetArea(target) is {} area)
            RaiseLocalEvent(area, ref ev);
    }

    /// <summary>
    /// Raises a by-ref event on the area at a given position.
    /// </summary>
    public void RaiseAreaEvent<T>(EntityCoordinates coords, ref T ev) where T: notnull
    {
        if (GetArea(coords) is {} area)
            RaiseLocalEvent(area, ref ev);
    }

    #endregion
}
