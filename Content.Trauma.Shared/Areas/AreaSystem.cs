// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Trauma.Shared.Areas;

/// <summary>
/// Tracks area prototypes and provides API for using them.
/// </summary>
public sealed class AreaSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly MapAreaSystem _mapArea = default!;
    [Dependency] private readonly EntityQuery<DepartmentAreaComponent> _deptQuery = default!;

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
    /// </summary>
    public EntityUid? GetArea(EntityCoordinates coords)
    {
        if (_transform.GetGrid(coords) is not {} grid)
            return null;

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
