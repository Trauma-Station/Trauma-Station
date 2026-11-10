// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Content.Shared.EntityEffects;
using Content.Shared.Interaction;
using Robust.Shared.EntitySerialization.Systems;

namespace Content.Trauma.Shared.Megafauna.Arena;

/// <summary>
/// System that handles teleporting you in, and out of megafauna arena grids.
/// Yes, this requires mapping to work.
/// </summary>
public sealed partial class TeleportArenaSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private MapLoaderSystem _loader = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [Dependency] private EntityQuery<TeleportFromArenaComponent> _tpFromQuery = default!;
    [Dependency] private EntityQuery<TeleportToArenaComponent> _tpToQuery = default!;

    private HashSet<Entity<TeleportFromArenaComponent>> _tpFrom = new();

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<TeleportToArenaComponent> ent, ref MapInitEvent args)
    {
        // Don't predict this lol, otherwise you will spawn fake maps
        if (_net.IsClient)
            return;

        // Load the grid on a dummy map
        _map.CreateMap(out var mapId);
        if (!_loader.TryLoadGrid(mapId, ent.Comp.ArenaPath, out var arena) || arena is not { } arenaGrid)
        {
            Log.Error($"Failed to load arena grid: {ent.Comp.ArenaPath}");
            _map.DeleteMap(mapId);
            return;
        }

        ent.Comp.Arena = arena;
        Dirty(ent);

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
        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            args.User,
            ent.Comp.Delay,
            new TeleportToArenaDoAfterEvent(),
            args.Target)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    [SubscribeLocalEvent]
    private void OnTeleportDoAfter(Entity<TeleportToArenaComponent> ent, ref TeleportToArenaDoAfterEvent args)
    {
        if (args.Cancelled || ent.Comp.Arena is not { } arena)
            return;

        var user = args.User;
        var xform = Transform(arena);

        _transform.SetCoordinates(user, xform.Coordinates);
        _transform.SetParent(user, arena); // set us parent of the grid, not the map

        if (ent.Comp.EffectsOnEnter is { } enterEffects)
            _effects.ApplyEffects(user, enterEffects);
    }

    [SubscribeLocalEvent]
    private void OnInteract(Entity<TeleportFromArenaComponent> ent, ref InteractHandEvent args)
    {
        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            args.User,
            ent.Comp.Delay,
            new TeleportFromArenaDoAfterEvent(),
            args.Target)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    [SubscribeLocalEvent]
    private void OnTeleportDoAfter(Entity<TeleportFromArenaComponent> ent, ref TeleportFromArenaDoAfterEvent args)
    {
        if (args.Cancelled || ent.Comp.Linked is not { } linked)
            return;

        var user = args.User;
        var xform = Transform(linked);

        _transform.SetCoordinates(user, xform.Coordinates);
        _transform.AttachToGridOrMap(user);

        if (GetExitEffects(linked) is { } exitEffects)
            _effects.ApplyEffects(user, exitEffects);
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

    /// <summary>
    /// Gets the exit effects of the entity.
    /// </summary>
    /// <returns>Null if there's no exit effects</returns>
    /// <remarks>
    /// This is called from the <see cref="TeleportFromArenaComponent"/> entity.
    /// </remarks>
    public EntityEffect[]? GetExitEffects(Entity<TeleportToArenaComponent?> ent)
    {
        if (!_tpToQuery.Resolve(ent.Owner, ref ent.Comp))
            return null;

        return ent.Comp.EffectsOnExit;
    }
}
