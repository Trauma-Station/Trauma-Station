// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Item;
using Content.Shared.Storage;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.Heretic.Systems;

public sealed partial class ClientEldritchPaperSystem : EldritchPaperSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedItemSystem _item = default!;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;
    [Dependency] private EntityQuery<ItemComponent> _itemQuery = default!;

    private static readonly ProtoId<ShaderPrototype> Shader = "Pixelate";
    private ShaderInstance _shader = default!;

    public override void Initialize()
    {
        base.Initialize();

        _shader = ProtoMan.Index(Shader).Instance();
    }

    // Rotate items based on their default "direction" in inventory grid to fit them better
    private readonly Dictionary<Direction, Angle> _itemDirectionRotations = new()
    {
        { Direction.North, Angle.Zero },
        { Direction.East, Angle.FromDegrees(90) },
        { Direction.South, Angle.FromDegrees(180) },
        { Direction.West, Angle.FromDegrees(-90) },
    };

    private readonly Dictionary<Direction, int> _itemDirectionSizes = new(4);

    private const string LayerMapKey = "eldritch_paper_";

    [SubscribeLocalEvent]
    private void OnAppearanceChange(Entity<EldritchPaperComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        ClearLayers((ent, sprite, ent.Comp));

        if (!Appearance.TryGetData<NetEntity>(ent, EldritchPaperVisuals.Item, out var netEnt) ||
            !TryGetEntity(netEnt, out var item) || !_spriteQuery.TryComp(item.Value, out var itemSprite))
            return;

        var now = _timing.CurTime;

        var rotation = Angle.Zero;
        if (_itemQuery.TryComp(item.Value, out var comp))
        {
            rotation = Angle.FromDegrees(-comp.StoredRotation);

            // Try to fit some sprites better based on their shape in inventory
            var shape = _item.GetItemShape((item.Value, comp)).GetBoundingBox();
            _itemDirectionSizes.Clear();
            _itemDirectionSizes[Direction.North] = shape.Top;
            _itemDirectionSizes[Direction.East] = shape.Right;
            _itemDirectionSizes[Direction.South] = shape.Bottom;
            _itemDirectionSizes[Direction.West] = shape.Left;
            var max = _itemDirectionSizes.MaxBy(x => x.Value).Key;
            rotation += _itemDirectionRotations[max];

            if (TryAddStoredItemTextureLayer((ent, sprite, ent.Comp), comp, now, rotation))
                return;
        }

        foreach (var spriteLayer in itemSprite.AllLayers)
        {
            if (spriteLayer is not SpriteComponent.Layer layer || !_sprite.IsVisible(layer))
                continue;

            var texture = layer.Texture ??
                (layer.ActualRsi?.Path is { } path && layer.State.Name is { } state
                    ? _sprite.GetFrame(new SpriteSpecifier.Rsi(path, state), now, layer.Loop)
                    : null);

            if (texture is not { } tex)
                continue;

            AddTextureLayer((ent, sprite, ent.Comp), tex, rotation);
        }
    }

    private bool TryAddStoredItemTextureLayer(Entity<SpriteComponent, EldritchPaperComponent> ent, ItemComponent comp, TimeSpan now, Angle rotation)
    {
        if (comp.StoredSprite is not { } storedSprite)
            return false;

        // I LOVE GetFrame throwing for paths that don't start with /Textures
        if (storedSprite is SpriteSpecifier.Rsi rsi && !rsi.RsiPath.CanonPath.StartsWith("/Textures"))
            storedSprite = new SpriteSpecifier.Rsi(new($"/Textures/{rsi.RsiPath}"), rsi.RsiState);

        var tex = _sprite.GetFrame(storedSprite, now);
        AddTextureLayer(ent, tex, rotation);
        return true;
    }

    private void AddTextureLayer(Entity<SpriteComponent, EldritchPaperComponent> ent, Texture texture, Angle rotation)
    {
        var nullableEnt = ent.AsNullable();
        var i = _sprite.AddTextureLayer(nullableEnt, texture);
        _sprite.LayerSetRotation(nullableEnt, i, rotation);
        _sprite.LayerSetScale(nullableEnt, i, ent.Comp2.ItemScale);
        _sprite.LayerMapSet(nullableEnt, $"{LayerMapKey}{ent.Comp2.AddedLayers}", i);

        ent.Comp1.LayerSetShader(i, _shader, Shader);

        ent.Comp2.AddedLayers++;
    }

    private void ClearLayers(Entity<SpriteComponent, EldritchPaperComponent> ent)
    {
        for (var i = 0; i < ent.Comp2.AddedLayers; i++)
        {
            if (!_sprite.LayerMapTryGet(ent.AsNullable(), $"{LayerMapKey}{i}", out var index, false))
                continue;

            _sprite.RemoveLayer(ent.AsNullable(), index, false);
        }

        ent.Comp2.AddedLayers = 0;
    }
}
