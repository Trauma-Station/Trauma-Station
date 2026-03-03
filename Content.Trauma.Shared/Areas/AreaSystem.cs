// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Coordinates.Helpers;
using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Areas;

/// <summary>
/// Tracks area prototypes and provides API for using them.
/// </summary>
public sealed class AreaSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private EntityQuery<DepartmentAreaComponent> _deptQuery;

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

        _deptQuery = GetEntityQuery<DepartmentAreaComponent>();

        SubscribeLocalEvent<AreaComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        LoadPrototypes();
    }

    private void OnAnchorStateChanged(Entity<AreaComponent> ent, ref AnchorStateChangedEvent args)
    {
        // delete areas that get unanchored by explosions, someone removing the floor etc
        // don't do it if client is detaching or it will break PVS
        if (!args.Anchored && !args.Detaching)
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
    /// Get the area at a given position.
    /// It will be snapped to the nearest tile, if your position is already snapped use <see cref="GetAreaCentered"/>.
    /// </summary>
    public EntityUid? GetArea(EntityCoordinates coords)
        => GetAreaCentered(coords.SnapToGrid(EntityManager, _map));

    /// <summary>
    /// Get the area at a given position which must be centered on a tile.
    /// Only call this if the coordinates are already centered on a tile.
    /// </summary>
    public EntityUid? GetAreaCentered(EntityCoordinates coords)
    {
        // TODO: if this is found to be expensive investigate:
        // A. storing which area(s) an entity is in through collisions (while map is unpaused)
        // B. having a quadtree etc to store areas instead of lookup
        // C. only using entities to map areas, store them on a special grid component similar to decals or tile air mixes
        _areas.Clear();
        _lookup.GetEntitiesInRange(coords, Range, _areas, Flags);
        foreach (var area in _areas)
        {
            return area; // return the first area, should only ever be 1 because of placement replacement
        }
        return null;
    }

    /// <summary>
    /// Get the department an area belongs to, or null if it lacks <see cref="DepartmentAreaComponent"/>.
    /// </summary>
    public ProtoId<DepartmentPrototype>? GetAreaDepartment(EntityUid area)
        => _deptQuery.CompOrNull(area)?.Department;

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
