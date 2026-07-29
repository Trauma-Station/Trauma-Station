// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Blob.Components;
using Content.Shared.Alert;
using Content.Shared.Actions;
using Content.Shared.ActionBlocker;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

namespace Content.Goobstation.Shared.Blob;

public abstract partial class SharedBlobObserverSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private BlobFactorySystem _factory = default!;
    [Dependency] private BlobNodeSystem _node = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedActionsSystem _action = default!;
    [Dependency] private SharedBlobCoreSystem _core = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] protected SharedTransformSystem Xform = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedViewSubscriberSystem _viewSub = default!;
    [Dependency] private EntityQuery<BlobCoreComponent> _coreQuery = default!;
    [Dependency] private EntityQuery<BlobTileComponent> _tileQuery = default!;

    private static readonly EntProtoId MobObserverBlobController = "MobObserverBlobController";
    private static readonly ProtoId<AlertPrototype> BlobHealth = "BlobHealth";
    private static readonly ProtoId<BlobTilePrototype> CoreTile = "Core";

    private HashSet<Entity<BlobTileComponent>> _tiles = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobObserverComponent, PlayerAttachedEvent>(OnPlayerAttached, before: [typeof(SharedActionsSystem)]);
        SubscribeLocalEvent<BlobObserverComponent, PlayerDetachedEvent>(OnPlayerDetached, before: [typeof(SharedActionsSystem)]);
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<BlobObserverComponent> ent, ref MapInitEvent args)
    {
        _hands.AddHand(ent.Owner, "BlobHand", HandLocation.Middle);

        ent.Comp.VirtualItem = PredictedSpawnAtPosition(MobObserverBlobController, Transform(ent).Coordinates);
        var comp = EnsureComp<BlobObserverControllerComponent>(ent.Comp.VirtualItem);
        comp.Blob = ent;
        Dirty(ent);

        if (!_hands.TryPickup(ent, ent.Comp.VirtualItem, "BlobHand", false, false, false))
            PredictedDel(ent.Comp.VirtualItem);
    }

    [SubscribeLocalEvent]
    private void OnGetUsedEntity(Entity<BlobObserverComponent> ent, ref GetUsedEntityEvent args)
    {
        if (ent.Comp.VirtualItem.Valid)
            args.Used = ent.Comp.VirtualItem;
    }

    private void OnPlayerAttached(Entity<BlobObserverComponent> ent, ref PlayerAttachedEvent args)
    {
        UpdateActions(args.Player, ent);
        if (ent.Comp.Core is { } core)
            _core.UpdateAllAlerts(core);
    }

    private void OnPlayerDetached(Entity<BlobObserverComponent> ent, ref PlayerDetachedEvent args)
    {
        if (ent.Comp.Core is { } core && !TerminatingOrDeleted(core))
        {
            _viewSub.RemoveViewSubscriber(core, args.Player);
        }
    }

    [SubscribeLocalEvent]
    private void OnBlobSwapChem(Entity<BlobCoreComponent> ent, ref BlobSwapChemActionEvent args)
    {
        var user = args.Performer;
        if (!TryComp<BlobObserverComponent>(user, out var comp))
            return;

        _ui.TryToggleUi(user, BlobChemSwapUiKey.Key, user);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnChemSelected(Entity<BlobObserverComponent> ent, ref BlobSetChemMessage args)
    {
        if (ent.Comp.Core is not { } core ||
            !_coreQuery.TryComp(core, out var coreComp) ||
            coreComp.CurrentChem == args.Chem ||
            !_core.TryUseAbility((core, coreComp), coreComp.SwapChemCost))
            return;

        _core.ChangeChem((core, coreComp), args.Chem);
    }

    [SubscribeLocalEvent]
    private void OnSplitCore(Entity<BlobCoreComponent> ent, ref BlobSplitCoreActionEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer;
        if (!ent.Comp.CanSplit)
        {
            _popup.PopupEntity(Loc.GetString("blob-cant-split"), user, user, PopupType.Large);
            return;
        }

        if (_core.GetTargetTile(args.Target) is not { } tile || !HasComp<BlobNodeComponent>(tile) || HasComp<BlobCoreComponent>(tile))
        {
            _popup.PopupEntity(Loc.GetString("blob-target-node-blob-invalid"), user, user, PopupType.Large);
            args.Handled = true;
            return;
        }

        if (!_core.TryUseAbility(ent.AsNullable(), ent.Comp.SplitCoreCost))
        {
            args.Handled = true;
            return;
        }

        PredictedQueueDel(tile);
        var coreTile = ProtoMan.Index(CoreTile);
        var newCore = PredictedSpawnAtPosition(coreTile.Entity, args.Target);

        ent.Comp.CanSplit = false;
        DirtyField(ent, ent.Comp, nameof(BlobCoreComponent.CanSplit));
        _action.RemoveAction(args.Action.AsNullable());

        if (TryComp<BlobCoreComponent>(newCore, out var newComp))
        {
            newComp.CanSplit = false;
            newComp.BlobTiles.Add(newCore);
            DirtyField(newCore, newComp, nameof(BlobCoreComponent.CanSplit));
        }

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnSwapCore(Entity<BlobCoreComponent> core, ref BlobSwapCoreActionEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer;
        if (_core.GetTargetTile(args.Target) is not { } tile || !TryComp<BlobNodeComponent>(tile, out var node))
        {
            _popup.PopupEntity(Loc.GetString("blob-target-node-blob-invalid"), user, user, PopupType.Large);
            args.Handled = true;
            return;
        }

        if (!_core.TryUseAbility(core.AsNullable(), core.Comp.SwapCoreCost))
        {
            args.Handled = true;
            return;
        }

        // Swap positions of blob's core and node.
        var xformNode = Transform(tile);
        var xformCore = Transform(core);
        var nodePos = xformNode.Coordinates;
        var corePos = xformCore.Coordinates;
        Xform.SetCoordinates(core, nodePos.SnapToGrid());
        Xform.SetCoordinates(tile, corePos.SnapToGrid());
        if (!xformCore.Anchored)
        {
            Xform.AnchorEntity(core, xformCore);
        }
        if (!xformNode.Anchored)
        {
            Xform.AnchorEntity(tile, xformNode);
        }

        // And then swap their linked special tiles
        _node.SwapSpecials(
            (tile, node),
            (core, EnsureComp<BlobNodeComponent>(core)));

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnCreateBlobbernaut(Entity<BlobCoreComponent> core, ref BlobCreateBlobbernautActionEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer;
        if (_core.GetTargetTile(args.Target) is not { } tile || !TryComp<BlobFactoryComponent>(tile, out var factory))
        {
            _popup.PopupEntity(Loc.GetString("blob-target-factory-blob-invalid"), user, user, PopupType.LargeCaution);
            return;
        }

        if (factory.HasBlobbernaut)
        {
            _popup.PopupEntity(Loc.GetString("blob-target-already-produce-blobbernaut"), user, user, PopupType.LargeCaution);
            return;
        }

        if (!_core.TryUseAbility(core.AsNullable(), core.Comp.BlobbernautCost, args.Target.AlignWithClosestGridTile()))
            return;

        // TODO: only consume points if it succeeds...
        if (!_factory.ProduceBlobbernaut((tile, factory)))
            return;

        _popup.PopupEntity(Loc.GetString("blob-spent-resource", ("point", core.Comp.BlobbernautCost)),
            tile,
            user,
            PopupType.LargeCaution);

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnBlobToCore(Entity<BlobCoreComponent> ent, ref BlobToCoreActionEvent args)
    {
        if (args.Handled)
            return;

        Xform.SetCoordinates(args.Performer, Transform(ent).Coordinates);
        args.Handled = true;
    }

    private void UpdateActions(ICommonSession player, Entity<BlobObserverComponent> ent)
    {
        if (ent.Comp.Core is not { } core || !_coreQuery.TryComp(core, out var coreComp))
        {
            Log.Error($"Tried to update actions for blob observer {ToPrettyString(ent)} with no core!");
            return;
        }

        _action.GrantActions(ent.Owner, coreComp.Actions, core);
        _viewSub.AddViewSubscriber(core, player); // keep the core in pvs, TODO: just a single override for the core should be enough?
    }

    public (EntityUid? nearestEntityUid, float nearestDistance) CalculateNearestBlobTileDistance(MapCoordinates position)
    {
        var nearestDistance = float.MaxValue;
        EntityUid? nearest = null;

        _tiles.Clear();
        _lookup.GetEntitiesInRange(position, 5f, _tiles);
        foreach (var tile in _tiles)
        {
            var tileCords = Xform.GetMapCoordinates(tile.Owner);
            var distance = Vector2.Distance(position.Position, tileCords.Position);

            if (distance >= nearestDistance)
                continue;

            nearestDistance = distance;
            nearest = tile;
        }

        return (nearest, nearestDistance);
    }
}
