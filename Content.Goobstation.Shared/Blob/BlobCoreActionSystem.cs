// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.CCVar;
using Content.Goobstation.Shared.Blob.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.EntityEffects;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.SubFloor;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Blob;

public sealed partial class BlobCoreActionSystem : EntitySystem
{
    [Dependency] private BlobTileSystem _tile = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private ITileDefinitionManager _tiles = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedBlobCoreSystem _core = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityQuery<BlobTileComponent> _tileQuery = default!;
    [Dependency] private EntityQuery<BlobCoreComponent> _coreQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;

    private bool _canGrowInSpace = true;

    public static readonly ProtoId<BlobTilePrototype> GrowthTile = "Normal";
    public static readonly ProtoId<ContentTileDefinition> Plating = "Plating";

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, GoobCVars.BlobCanGrowInSpace, value => _canGrowInSpace = value, true);
    }

    private void BlobInteract(Entity<BlobObserverComponent> observer, Entity<BlobCoreComponent> core, InteractEvent args)
    {
        if (TerminatingOrDeleted(observer) || TerminatingOrDeleted(core))
            return;

        var location = args.ClickLocation.AlignWithClosestGridTile(entityManager: EntityManager);

        if (!location.IsValid(EntityManager))
            return;

        var gridUid = _transform.GetGrid(location);

        if (!_gridQuery.TryComp(gridUid, out var grid))
            return;

        var fromTile = FindNearBlobTile(location, (gridUid.Value, grid));

        #region OnTarget
        if (args.Target is { } target && !HasComp<BlobMobComponent>(target))
        {
            if (_tileQuery.TryComp(target, out var tileComp) && tileComp.Core != null)
                return;

            if (fromTile != null && HasComp<DestructibleComponent>(target) && !HasComp<ItemComponent>(target) && !HasComp<SubFloorHideComponent>(target))
            {
                BlobTargetAttack(core, fromTile.Value, target, args.User);
                return;
            }
        }
        #endregion

        var targetTile = _map.GetTileRef(gridUid.Value, grid, location);

        var targetTileEmpty = false;
        if (targetTile.Tile.IsEmpty)
        {
            if (!_canGrowInSpace)
                return;

            targetTileEmpty = true;
        }

        if (_map.GetAnchoredEntities(gridUid.Value, grid, targetTile.GridIndices).Any(_tileQuery.HasComponent))
            return;

        var node = _core.GetNearNode(location, core.Comp.TilesRadiusLimit);

        if (fromTile != null && node == null)
            _popup.PopupCoordinates(Loc.GetString("blob-target-nearby-not-node"), location, args.User, PopupType.Large);

        if (fromTile == null || node == null)
            return;

        var placing = ProtoMan.Index(GrowthTile);
        var cost = placing.Cost;
        if (targetTileEmpty)
        {
            // 2.5x
            cost *= 5;
            cost /= 2;
        }

        if (!_core.TryUseAbility(core.AsNullable(), cost, location))
            return;

        if (targetTileEmpty)
        {
            var plating = _tiles[Plating];
            var platingTile = new Tile(plating.TileId);
            _map.SetTile(gridUid.Value, grid, location, platingTile);
        }

        _core.TransformBlobTile(null,
            core.AsNullable(),
            node,
            placing.ID,
            location);

        core.Comp.NextAction = _timing.CurTime + _cooldown + TimeSpan.FromSeconds(Math.Abs(core.Comp.GrowRate));
        DirtyField(core, core.Comp, nameof(BlobCoreComponent.NextAction));
    }

    private EntityUid? FindNearBlobTile(EntityCoordinates coords, Entity<MapGridComponent> grid)
    {
        var mobTile = _map.GetTileRef(grid, grid, coords);
        var center = mobTile.GridIndices;

        var adjacentTiles = new[]
        {
            center.Offset(Direction.East),
            center.Offset(Direction.West),
            center.Offset(Direction.North),
            center.Offset(Direction.South),
        };

        foreach (var indices in adjacentTiles)
        {
            foreach (var ent in _map.GetAnchoredEntities(grid, grid, indices))
            {
                if (_tileQuery.CompOrNull(ent)?.Core != null)
                    return ent;
            }
        }

        return null;
    }

    private void BlobTargetAttack(Entity<BlobCoreComponent> ent, Entity<BlobTileComponent?> from, EntityUid target, EntityUid user)
    {
        if (ent.Comp.Observer == null)
            return;

        if (!_core.TryUseAbility(ent.AsNullable(), ent.Comp.AttackCost, Transform(target).Coordinates))
            return;

        var chem = ProtoMan.Index(ent.Comp.CurrentChem);
        _tile.DoLunge(from, target, user);
        _damage.ChangeDamage(target, chem.Damage);

        if (chem.AttackEffects is { } effects)
            _effects.ApplyEffects(target, effects, user: ent.Comp.Observer, predicted: false);

        ent.Comp.NextAction = _timing.CurTime + _cooldown + TimeSpan.FromSeconds(Math.Abs(ent.Comp.AttackRate));
        DirtyField(ent, ent.Comp, nameof(BlobCoreComponent.NextAction));
        _audio.PlayPvs(ent.Comp.AttackSound, from, AudioParams.Default);
    }

    private static readonly TimeSpan _cooldown = TimeSpan.FromMilliseconds(333);

    private void OnInteract(Entity<BlobObserverComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == args.User)
            return;

        if (ent.Comp.Core is not { } core ||
            !_coreQuery.TryComp(core, out var coreComp))
            return;

        var now = _timing.CurTime;
        if (now < coreComp.NextAction)
            return;

        var location = args.ClickLocation;
        if (!location.IsValid(EntityManager))
            return;

        args.Handled = true;
        coreComp.NextAction = now + _cooldown;
        DirtyField(core, coreComp, nameof(BlobCoreComponent.NextAction));

        BlobInteract(ent, (core, coreComp), args);
    }

    [SubscribeLocalEvent]
    private void OnInteractTarget(Entity<BlobObserverComponent> ent, ref UserActivateInWorldEvent args)
    {
        var ev = new AfterInteractEvent(args.User, EntityUid.Invalid, args.Target, Transform(args.Target).Coordinates, true);
        OnInteract(ent, ref ev); // proxy?
        args.Handled = ev.Handled;
    }

    [SubscribeLocalEvent]
    private void OnInteractController(Entity<BlobObserverControllerComponent> ent, ref AfterInteractEvent args)
    {
        if (!TryComp<BlobObserverComponent>(ent.Comp.Blob, out var blob))
            return;

        var ev = new AfterInteractEvent(args.User, EntityUid.Invalid, args.Target, args.ClickLocation, true);
        OnInteract((ent.Comp.Blob, blob), ref ev); // proxy?
        args.Handled = ev.Handled;
    }
}
