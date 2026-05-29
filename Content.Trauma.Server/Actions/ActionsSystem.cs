// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.DoAfter;
using Content.Trauma.Shared.Actions;
using Content.Trauma.Shared.Actions.Events;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Trauma.Server.Actions;

public sealed partial class ActionsSystem : EntitySystem
{
    [Dependency] private ITileDefinitionManager _tileDef = default!;

    [Dependency] private AnchorableSystem _anchorable = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private DoAfterSystem _doAfter = default!;
    [Dependency] private MapSystem _mapSystem = default!;
    [Dependency] private PlasmaCostActionSystem _plasmaCost = default!;
    [Dependency] private TransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SpawnTileEntityActionEvent>(OnSpawnTileEntityAction);
        SubscribeLocalEvent<PlaceTileEntityEvent>(OnPlaceTileEntityEvent);

        SubscribeLocalEvent<PlaceTileEntityDoAfterEvent>(OnPlaceTileEntityDoAfter);
    }

    private void OnSpawnTileEntityAction(SpawnTileEntityActionEvent args)
    {
        if (!args.Handled && CreationTileEntity(args.Performer, args.Performer.ToCoordinates(), args.TileId, args.Entity, args.Audio, args.BlockedCollisionLayer, args.BlockedCollisionMask))
            args.Handled = true;
    }

    private void OnPlaceTileEntityEvent(PlaceTileEntityEvent args)
    {
        if (args.Handled)
            return;

        // Check if this is a plasma-cost action and get the cost
        // Goobstation
        TryComp<PlasmaCostActionComponent>(args.Action, out var plasmaCost);
        var plasmaCostValue = plasmaCost?.PlasmaCost ?? FixedPoint2.Zero;

        if (args.Length != 0)
        {
            if (CheckTileBlocked(args.Target, args.BlockedCollisionLayer, args.BlockedCollisionMask))
                return;

            var ev = new PlaceTileEntityDoAfterEvent
            {
                Target = GetNetCoordinates(args.Target),
                Entity = args.Entity,
                TileId = args.TileId,
                Audio = args.Audio,
                BlockedCollisionLayer = args.BlockedCollisionLayer,
                BlockedCollisionMask = args.BlockedCollisionMask, // Goobstation start
                PlasmaCost = plasmaCostValue,
                Action = GetNetEntity(args.Action) // Goobstation end
            };

            var doAfter = new DoAfterArgs(EntityManager, args.Performer, args.Length, ev, null)
            {
                BlockDuplicate = true,
                BreakOnDamage = true,
                BreakOnMove = true, // Goobstation start
                NeedHand = false,
                CancelDuplicate = true, // Gooobstation end
                Broadcast = true
            };

            _doAfter.TryStartDoAfter(doAfter);
            return;
        }

        if (CreationTileEntity(args.Performer, args.Target, args.TileId, args.Entity, args.Audio, args.BlockedCollisionLayer, args.BlockedCollisionMask))
            args.Handled = true;
    }

    /// Goobstation
    /// <summary>
    /// Handles the placement of a tile entity after the placement action is confirmed.
    /// Verifies plasma cost and creates the tile if conditions are met.
    /// </summary>
    /// <param name="args">Event data containing placement details and cost</param>
    private void OnPlaceTileEntityDoAfter(PlaceTileEntityDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        // Check plasma cost only when the action is about to complete
        if (!_plasmaCost.HasEnoughPlasma(args.User, args.PlasmaCost))
            return;

        _plasmaCost.DeductPlasma(args.User, args.PlasmaCost);

        if (CreationTileEntity(args.User, GetCoordinates(args.Target), args.TileId, args.Entity, args.Audio, args.BlockedCollisionLayer, args.BlockedCollisionMask))
            args.Handled = true;
    }

    #region Helpers

    private bool CreationTileEntity(EntityUid user, EntityCoordinates coordinates, string? tileId, EntProtoId? entProtoId, SoundSpecifier? audio, int collisionLayer = 0, int collisionMask = 0)
    {
        if (_container.IsEntityOrParentInContainer(user))
            return false;

        if (tileId != null)
        {
            if (_transform.GetGrid(coordinates) is not { } grid || !TryComp(grid, out MapGridComponent? mapGrid))
                return false;

            var tileDef = _tileDef[tileId];
            var tile = new Tile(tileDef.TileId);

            _mapSystem.SetTile(grid, mapGrid, coordinates, tile);
        }

        _audio.PlayPvs(audio, coordinates);

        if (entProtoId == null || CheckTileBlocked(coordinates, collisionLayer, collisionMask))
            return false;

        Spawn(entProtoId, coordinates);

        return true;
    }

    private bool CheckTileBlocked(EntityCoordinates coordinates, int collisionLayer = 0, int collisionMask = 0)
    {
        if (_transform.GetGrid(coordinates) is not { } grid || !TryComp(grid, out MapGridComponent? mapGrid))
            return true;

        var tileIndices = _mapSystem.TileIndicesFor(grid, mapGrid, coordinates);
        return !_anchorable.TileFree(mapGrid, tileIndices, collisionLayer, collisionMask);
    }

    #endregion
}
