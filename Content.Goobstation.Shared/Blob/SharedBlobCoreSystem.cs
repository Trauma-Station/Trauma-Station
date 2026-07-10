// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Shared.Blob.Components;
using Content.Goobstation.Shared.Blob.Events;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.Explosion.Components;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

namespace Content.Goobstation.Shared.Blob;

public abstract partial class SharedBlobCoreSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private BlobTileSystem _tile = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private SharedActionsSystem _action = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityQuery<BlobCoreComponent> _query = default!;
    [Dependency] private EntityQuery<BlobFactoryComponent> _factoryQuery = default!;
    [Dependency] private EntityQuery<BlobNodeComponent> _nodeQuery = default!;
    [Dependency] protected EntityQuery<BlobTileComponent> TileQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;

    private static readonly ProtoId<AlertPrototype> BlobHealth = "BlobHealth";
    private static readonly ProtoId<AlertPrototype> BlobResource = "BlobResource";

    #region Events

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<BlobCoreComponent> ent, ref MapInitEvent args)
    {
        if (!TileQuery.TryComp(ent, out var tile) ||
            !_nodeQuery.TryComp(ent, out var node))
            return;

        ConnectBlobTile((ent, tile), ent.AsNullable(), (ent, node));

        UpdateAllAlerts(ent.AsNullable());
        UpdateChem(ent);

        foreach (var actionId in ent.Comp.ActionPrototypes)
        {
            if (_action.AddAction(ent.Owner, actionId) is { } action)
                ent.Comp.Actions.Add(action);
        }
        DirtyField(ent, ent.Comp, nameof(BlobCoreComponent.Actions));
    }

    [SubscribeLocalEvent]
    private void OnDamaged(Entity<BlobCoreComponent> ent, ref DamageChangedEvent args)
    {
        UpdateAllAlerts(ent.AsNullable());
    }

    [SubscribeLocalEvent]
    private void OnTileTransform(Entity<BlobCoreComponent> ent, ref BlobTransformTileActionEvent args)
    {
        TransformSpecialTile(ent, args);
    }

    #endregion

    public void UpdateAllAlerts(Entity<BlobCoreComponent?> core)
    {
        if (!Resolve(core, ref core.Comp))
            return;

        if (core.Comp.Observer is not { } user)
            return;

        // This one for points
        var pt = (float) core.Comp.CurrentPoints;
        var pointsSeverity = (short) Math.Clamp(Math.Round(pt * 0.1f), 0, 51);
        _alerts.ShowAlert(user, BlobResource, pointsSeverity);

        // And this one for health.
        var total = _damage.GetTotalDamage(core.Owner);
        var currentHealth = core.Comp.CoreBlobTotalHealth - total;
        var healthSeverity = (short) Math.Clamp(Math.Round(currentHealth.Float() / 20f), 0, 20);
        _alerts.ShowAlert(user, BlobHealth, healthSeverity);
    }

    public void ChangeChem(Entity<BlobCoreComponent?> ent, ProtoId<BlobChemPrototype> newChem)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (newChem == ent.Comp.CurrentChem)
            return;

        ent.Comp.CurrentChem = newChem;
        DirtyField(ent, ent.Comp, nameof(BlobCoreComponent.CurrentChem));

        UpdateChem(ent.Comp);
    }

    private void UpdateChem(BlobCoreComponent core)
    {
        var chem = ProtoMan.Index(core.CurrentChem);
        var color = chem.Color;
        var nautDamage = chem.Damage * 0.8f;
        foreach (var tile in core.BlobTiles)
        {
            if (!TileQuery.TryComp(tile, out var tileComp))
                continue;

            tileComp.Color = color;
            Dirty(tile, tileComp);

            ChangeBlobEntChem((tile, tileComp), chem);

            if (_factoryQuery.CompOrNull(tile)?.Blobbernaut is not { } mob)
                continue;

            if (!TryComp<BlobbernautComponent>(mob, out var naut))
                continue;

            naut.CurrentChem = core.CurrentChem;
            Dirty(mob, naut);

            if (TryComp<MeleeWeaponComponent>(mob, out var melee))
            {
                melee.Damage = nautDamage;
                Dirty(mob, melee);
            }

            ChangeBlobEntChem(mob, chem);
        }
    }

    private void ChangeBlobEntChem(Entity<BlobTileComponent?> ent, BlobChemPrototype chem)
    {
        if (ProtoMan.TryIndex(ent.Comp?.Tile, out var tile) && !tile.CanChangeChem)
            return;

        _damage.SetDamageModifierSetId(ent.Owner, chem.DamageModifiers);
        var expRes = chem.ExplosionResistance;
        if (expRes == 0f)
        {
            RemComp<ExplosionResistanceComponent>(ent);
        }
        else
        {
            var res = EnsureComp<ExplosionResistanceComponent>(ent);
            res.DamageCoefficient = 1f - expRes; // damage % is inversely proportional to resistance
            Dirty(ent, res);
        }
    }

    /// <summary>
    /// Transforms one blob tile in another type or creates a new one from scratch.
    /// </summary>
    /// <param name="oldTile">Uid of the ols tile that's going to get deleted.</param>
    /// <param name="core">Blob core that preformed the transformation. Make sure it isn't came from the BlobTileComponent of the target!</param>
    /// <param name="node">Node will be used in ConnectBlobTile method.</param>
    /// <param name="id">Type of a new blob tile.</param>
    /// <param name="coords">Coordinates of a new tile.</param>
    /// <seealso cref="ConnectBlobTile"/>
    /// <seealso cref="BlobCoreComponent"/>
    public bool TransformBlobTile(
        Entity<BlobTileComponent?>? oldTile,
        Entity<BlobCoreComponent?> core,
        Entity<BlobNodeComponent>? node,
        [ForbidLiteral] ProtoId<BlobTilePrototype> id,
        EntityCoordinates coords)
    {
        if (!Resolve(core, ref core.Comp))
            return false;

        if (oldTile is { } old)
        {
            if (!Resolve(old, ref old.Comp) || old.Comp.Core != core.Owner)
                return false;

            PredictedQueueDel(old);
        }

        var proto = ProtoMan.Index(id);
        var tile = PredictedSpawnAtPosition(proto.Entity, coords);
        var tileComp = TileQuery.Comp(tile);

        ConnectBlobTile((tile, tileComp), core.AsNullable(), node);
        ChangeBlobEntChem((tile, tileComp), ProtoMan.Index(core.Comp.CurrentChem));

        return true;
    }

    /// <summary>
    /// Adds BlobTile to blob core and node, if specified.
    /// </summary>
    /// <param name="tile">Entity of the blob tile.</param>
    /// <param name="core">Entity of the blob core.</param>
    /// <param name="node">If not null, tries to connect tile to the node by if required.</param>
    public void ConnectBlobTile(
        Entity<BlobTileComponent> tile,
        Entity<BlobCoreComponent?> core,
        Entity<BlobNodeComponent>? node)
    {
        if (!_query.Resolve(core, ref core.Comp))
            return;

        core.Comp.BlobTiles.Add(tile);

        tile.Comp.Color = ProtoMan.Index(core.Comp.CurrentChem).Color;
        tile.Comp.Core = core;
        Dirty(tile);

        if (node == null)
            return;

        switch (tile.Comp.Tile)
        {
            case "Factory":
                node.Value.Comp.BlobFactory = tile;
                break;
            case "Resource":
                node.Value.Comp.BlobResource = tile;
                break;
            default:
                return;
        }
        Dirty(node.Value);
    }

    public Entity<BlobTileComponent>? GetTargetTile(EntityCoordinates coords)
    {
        if (_transform.GetGrid(coords) is not { } gridUid ||
            !_gridQuery.TryComp(gridUid, out var gridComp))
            return null;

        foreach (var ent in _map.GetAnchoredEntities((gridUid, gridComp), coords))
        {
            if (!TileQuery.TryComp(ent, out var tile))
                continue;

            return (ent, tile);
        }

        return null;
    }

    public bool CheckValidBlobTile(
        Entity<BlobTileComponent> tile,
        Entity<BlobNodeComponent>? node,
        bool requireNode,
        BlobTransformTileActionEvent args)
    {
        var coords = Transform(tile).Coordinates;

        var newTile = args.TileType;
        var checkTile = args.TransformFrom;
        var user = args.Performer;

        if (tile.Comp.Core is not { } core ||
            !_query.TryComp(core, out var coreComp) ||
            tile.Comp.Tile == newTile ||
            checkTile != null && tile.Comp.Tile != checkTile)
        {
            _popup.PopupCoordinates(Loc.GetString("blob-target-normal-blob-invalid"), coords, user, PopupType.Large);
            return false;
        }

        // Handle node spawn
        if (ProtoMan.Index(newTile).BlockNearNodes)
        {
            if (GetNearNode(coords, coreComp.NodeRadiusLimit) == null)
                return true;

            _popup.PopupCoordinates(Loc.GetString("blob-target-close-to-node"), coords, user, PopupType.Large);
            return false;
        }

        if (!requireNode)
            return true;

        if (node == null)
        {
            _popup.PopupCoordinates(Loc.GetString("blob-target-nearby-not-node"),
                coords,
                user,
                PopupType.Large);
            return false;
        }

        if (_tile.IsEmptySpecial(node.Value, newTile))
            return true;

        _popup.PopupCoordinates(Loc.GetString("blob-target-already-connected"),
            coords,
            user,
            PopupType.Large);
        return false;
    }

    public void TransformSpecialTile(Entity<BlobCoreComponent> core, BlobTransformTileActionEvent args)
    {
        if (GetTargetTile(args.Target) is not { } tile || tile.Comp.Core == null)
            return;

        var coords = Transform(tile).Coordinates;
        var tileType = args.TileType;
        var node = GetNearNode(coords);

        if (!CheckValidBlobTile(tile, node, args.RequireNode, args))
            return;

        TransformBlobTile(
            tile.AsNullable(),
            core.AsNullable(),
            node,
            tileType,
            coords);
    }

    public bool ChangeBlobPoint(Entity<BlobCoreComponent> core, int amount)
    {
        var next = core.Comp.CurrentPoints + amount;
        if (amount == 0 || next < 0) // no blob overdraft
            return false;

        core.Comp.CurrentPoints = next;
        DirtyField(core, core.Comp, nameof(BlobCoreComponent.CurrentPoints));
        UpdateAllAlerts(core.AsNullable());
        return true;
    }

    /// <summary>
    /// Writes off points for some blob core and creates popup on observer or specified coordinates.
    /// </summary>
    /// <param name="core">Blob core that is going to lose points.</param>
    /// <param name="abilityCost">Cost of the ability.</param>
    /// <param name="coordinates">If not null, coordinates for popup to appear.</param>
    public bool TryUseAbility(Entity<BlobCoreComponent?> core, int abilityCost, EntityCoordinates? coordinates = null)
    {
        if (!Resolve(core, ref core.Comp))
            return false;

        var observer = core.Comp.Observer;
        var money = core.Comp.CurrentPoints;

        if (observer == null)
            return false;

        if (money < abilityCost)
        {
            _popup.PopupEntity(Loc.GetString(
                "blob-not-enough-resources",
                ("point", abilityCost - money)),
                observer.Value,
                observer.Value,
                PopupType.Large);
            return false;
        }

        coordinates ??= Transform(observer.Value).Coordinates;

        _popup.PopupCoordinates(
            Loc.GetString("blob-spent-resource", ("point", abilityCost)),
            coordinates.Value,
            observer.Value,
            PopupType.LargeCaution);

        ChangeBlobPoint((core, core.Comp), -abilityCost);
        return true;
    }

    /// <summary>
    /// Gets the nearest Blob node from some EntityCoordinates.
    /// </summary>
    /// <param name="coords">The EntityCoordinates to check from.</param>
    /// <param name="radius">Radius to check from coords.</param>
    /// <returns>Nearest blob node with it's component, null if wasn't founded.</returns>
    public Entity<BlobNodeComponent>? GetNearNode(
        EntityCoordinates coords,
        float radius = 3f)
    {
        if (_transform.GetGrid(coords) is not { } gridUid ||
            !_gridQuery.TryComp(gridUid, out var grid))
            return null;

        var nearestDistance = float.MaxValue;
        var nodeComponent = new BlobNodeComponent();
        Entity<BlobNodeComponent>? nearest = default;

        var innerTiles = _map.GetLocalTilesIntersecting(
                gridUid,
                grid,
                new Box2(coords.Position + new Vector2(-radius, -radius),
                    coords.Position + new Vector2(radius, radius)),
                false)
            .ToArray();

        foreach (var tileRef in innerTiles)
        {
            foreach (var ent in _map.GetAnchoredEntities(gridUid, grid, tileRef.GridIndices))
            {
                if (!_nodeQuery.TryComp(ent, out var nodeComp))
                    continue;

                var tileCords = Transform(ent).Coordinates;
                var distance = Vector2.DistanceSquared(coords.Position, tileCords.Position);
                if (distance >= nearestDistance)
                    continue;

                nearestDistance = distance;
                nearest = (ent, nodeComp);
            }
        }

        return nearest;
    }
}
