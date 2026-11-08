// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Interaction;
using Robust.Shared.EntitySerialization.Systems;

namespace Content.Trauma.Shared.Megafauna.Arena;

/// <summary>
/// System that handles teleporting you in, and out of megafauna arena grids.
/// Yes, this requires mapping to work.
/// </summary>
public sealed partial class TeleportArenaSystem : EntitySystem
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private MapLoaderSystem _loader = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [Dependency] private EntityQuery<TeleportFromArenaComponent> _tpFromQuery = default!;

    private HashSet<Entity<TeleportFromArenaComponent>> _tpFrom = new();

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<TeleportToArenaComponent> ent, ref MapInitEvent args)
    {
        // Load the grid on a dummy map, and link all entities with teleport from arena comp. with us.
        _map.CreateMap(out var mapId);
        if (!_loader.TryLoadGrid(mapId, ent.Comp.ArenaPath, out var arena))
        {
            Log.Error($"Failed to load arena grid: {ent.Comp.ArenaPath}");
            return;
        }

        if (arena is not { } arenaGrid)
            return;

        _tpFrom.Clear();
        _lookup.GetGridEntities(arenaGrid, _tpFrom);
        foreach (var tpFrom in _tpFrom)
        {
            Link(tpFrom.AsNullable(), ent);
        }
    }

    [SubscribeLocalEvent]
    private void OnInteract(Entity<TeleportToArenaComponent> ent, ref InteractHandEvent args)
    {
        if (ent.Comp.Arena is not { } arena)
            return;

        var xform = Transform(arena);
        _transform.SetCoordinates(args.User, xform.Coordinates);
    }

    [SubscribeLocalEvent]
    private void OnInteract(Entity<TeleportFromArenaComponent> ent, ref InteractHandEvent args)
    {
        if (ent.Comp.Linked is not { } linked)
            return;

        var user = args.User;
        var xform = Transform(linked);

        _transform.SetCoordinates(user, xform.Coordinates);
        _transform.AttachToGridOrMap(user);
    }

    /// <summary>
    /// Links an entity to this one, so when interacted with, we teleport to it
    /// </summary>
    public void Link(Entity<TeleportFromArenaComponent?> ent, EntityUid target)
    {
        if (!_tpFromQuery.Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.Linked = target;
        Dirty(ent);
    }
}
