// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Wounds;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Blade;
using Content.Trauma.Shared.Heretic.Events;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Trauma.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem
{
    private readonly List<TileRef> _tilesToConvert = new();

    protected override void SubscribeBlade()
    {
        base.SubscribeBlade();

        SubscribeLocalEvent<HereticChampionStanceEvent>(OnChampionStance);
        SubscribeLocalEvent<EventHereticFuriousSteel>(OnFuriousSteel);
        SubscribeLocalEvent<EventHereticDomainExpansion>(OnDomainExpansion);
    }

    private void OnDomainExpansion(EventHereticDomainExpansion args)
    {
        DebugTools.Assert(args.MinRadius <= args.TileRadius);

        var uid = args.Performer;

        if (!TryUseAbility(args, false) || !Heretic.TryGetHereticComponent(uid, out var heretic, out var mind))
            return;

        var coords = _transform.GetMapCoordinates(uid);

        var query = EntityQueryEnumerator<BladeArenaComponent, TransformComponent>();
        while (query.MoveNext(out var otherArena, out _, out var xform))
        {
            var mapCoords = _transform.GetMapCoordinates(otherArena, xform);
            if (mapCoords.MapId != coords.MapId)
                continue;

            if ((mapCoords.Position - coords.Position).Length() > args.TileRadius * 2.5f)
                continue;

            Popup.PopupEntity(Loc.GetString("heretic-ability-fail-arena-nearby"), uid, uid);
            return;
        }

        if (!_mapMan.TryFindGridAt(coords, out var grid, out var gridComp))
        {
            FailPopup();
            return;
        }

        var center = _map.TileIndicesFor(grid, gridComp, coords);
        if (!_map.TryGetTileRef(grid, gridComp, center, out var centerTile))
        {
            FailPopup();
            return;
        }

        _tilesToConvert.Clear();
        _tilesToConvert.Add(centerTile);

        var max = GetGreatestDistAndTiles();

        if (max < args.MinRadius)
        {
            FailPopup();
            return;
        }

        args.Handled = true;

        var replacement = Proto.Index(args.TileReplacement);

        var arena = EntityManager.CreateEntityUninitialized(args.Arena, coords);
        var comp = EnsureComp<BladeArenaComponent>(arena);
        comp.Radius = max;
        comp.Grid = grid;
        EntityManager.InitializeAndStartEntity(arena);

        heretic.Minions.Add(arena);

        comp.TilesToRestore.Clear();
        foreach (var tile in _tilesToConvert)
        {
            comp.TilesToRestore.Add(tile.GridIndices);
            _tile.ReplaceTile(tile, replacement, grid, gridComp, ignoreLimit: true);
        }

        return;

        int GetGreatestDistAndTiles()
        {
            var greatestDist = 0;

            for (var i = 1; i <= args.TileRadius; i++)
            {
                for (var j = -i; j < i; j++)
                {
                    if (!_map.TryGetTileRef(grid, gridComp, center + new Vector2i(j, i), out var tile1) ||
                        !_map.TryGetTileRef(grid, gridComp, center + new Vector2i(i, -j), out var tile2) ||
                        !_map.TryGetTileRef(grid, gridComp, center + new Vector2i(-j, -i), out var tile3) ||
                        !_map.TryGetTileRef(grid, gridComp, center + new Vector2i(-i, j), out var tile4))
                        return greatestDist;

                    _tilesToConvert.Add(tile1);
                    _tilesToConvert.Add(tile2);
                    _tilesToConvert.Add(tile3);
                    _tilesToConvert.Add(tile4);
                }

                greatestDist++;
            }

            return greatestDist;
        }

        void FailPopup()
        {
            Popup.PopupEntity(Loc.GetString("heretic-ability-fail-not-enough-space"), uid, uid);
        }
    }

    private void OnChampionStance(HereticChampionStanceEvent args)
    {
        foreach (var part in _body.GetOrgans<WoundableComponent>(args.Heretic))
        {
            part.Comp.CanRemove = args.Negative;
            Dirty(part);
        }
    }

    private void OnFuriousSteel(EventHereticFuriousSteel args)
    {
        if (!TryUseAbility(args))
            return;

        StatusNew.TryUpdateStatusEffectDuration(args.Performer, args.StatusEffect, out _, args.StatusDuration);
    }
}
