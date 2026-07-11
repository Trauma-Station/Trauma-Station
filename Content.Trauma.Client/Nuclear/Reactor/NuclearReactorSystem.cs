// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Nuclear.Reactor;
using Robust.Client.ResourceManagement;
using Robust.Shared.Containers;

namespace Content.Trauma.Client.Nuclear.Reactor;

public sealed partial class NuclearReactorSystem : SharedNuclearReactorSystem
{
    [Dependency] private IResourceCache _res = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private EntityQuery<NuclearReactorComponent> _query = default!;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;

    private static readonly ResPath RsiPath = new("/Textures/_FarHorizons/Structures/Power/Generation/FissionGenerator/reactor_component_cap.rsi");
    private const string EmptyState = "empty_cap";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NuclearReactorComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<NuclearReactorComponent, EntInsertedIntoContainerMessage>(OnPartInserted);
        SubscribeLocalEvent<NuclearReactorComponent, EntRemovedFromContainerMessage>(OnPartRemoved);

        SubscribeLocalEvent<ReactorPartComponent, AfterAutoHandleStateEvent>(OnAutoHandleState);
    }

    private void OnStartup(Entity<NuclearReactorComponent> ent, ref ComponentStartup args)
    {
        var (uid, comp) = ent;
        if (!_spriteQuery.TryComp(uid, out var sprite))
            return;

        if (!_res.TryGetResource<RSIResource>(RsiPath, out var resource))
            return;

        var xspace = comp.Gridbounds[0] / 32f;
        var yspace = comp.Gridbounds[1] / 32f;
        var xoff = comp.Gridbounds[2] / 32f;
        var yoff = comp.Gridbounds[3] / 32f;

        var gridWidth = comp.GridWidth;
        var gridHeight = comp.GridHeight;

        var xAdj = (gridWidth - 1) / 2f;
        var yAdj = (gridHeight - 1) / 2f;

        var rsi = resource.RSI;
        Entity<SpriteComponent?> entSprite = (uid, sprite);
        for (var x = 0; x < gridWidth; x++)
        {
            for (var y = 0; y < gridHeight; y++)
            {
                var layerID = _sprite.AddRsiLayer(entSprite, EmptyState, rsi);
                _sprite.LayerMapSet(entSprite, FormatMap(new(x, y)), layerID);
                _sprite.LayerSetOffset(entSprite, layerID, new((xspace * (x - xAdj)) - xoff, (-yspace * (y - yAdj)) - yoff));
                _sprite.LayerSetColor(entSprite, layerID, Color.Black);
            }
        }
    }

    private static string FormatMap(Vector2i pos) => $"NuclearReactorCap_{pos.X}_{pos.Y}";

    private void OnPartInserted(Entity<NuclearReactorComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container != ent.Comp.PartsContainer || !PartQuery.TryComp(args.Entity, out var part) || part.Position is not { } pos)
            return;

        var color = PropsQuery.Comp(ent).Color;
        var map = FormatMap(pos);
        UpdateRodAppearance(ent.Owner, map, part.IconStateCap, color);
    }

    private void OnPartRemoved(Entity<NuclearReactorComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container != ent.Comp.PartsContainer || !PartQuery.TryComp(args.Entity, out var part) || part.Position is not { } pos)
            return;

        var map = FormatMap(pos);
        UpdateRodAppearance(ent.Owner, map, EmptyState, Color.Black);
    }

    private void OnAutoHandleState(Entity<ReactorPartComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        // ignore parts that arent installed in a reactor
        var parent = Transform(ent).ParentUid;
        if (ent.Comp.Position is not { } pos ||
            !_query.TryComp(parent, out var reactor) ||
            !reactor.PartsContainer.Contains(ent.Owner))
            return;

        var color = PropsQuery.Comp(ent).Color;
        var map = FormatMap(pos);
        UpdateRodAppearance(parent, map, ent.Comp.IconStateCap, color);
    }

    private void UpdateRodAppearance(Entity<SpriteComponent?> ent, string map, string state, Color color)
    {
        if (!_spriteQuery.Resolve(ent, ref ent.Comp))
            return;

        if (!_sprite.LayerMapTryGet(ent, map, out var layer, false))
            return;

        _sprite.LayerSetRsiState(ent, layer, state);
        _sprite.LayerSetColor(ent, layer, color);
    }
}
