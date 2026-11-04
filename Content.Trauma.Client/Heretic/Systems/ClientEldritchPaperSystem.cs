// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Item;
using Content.Shared.Storage;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Systems;

namespace Content.Trauma.Client.Heretic.Systems;

public sealed partial class ClientEldritchPaperSystem : EldritchPaperSystem
{
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

            if (TryAddStoredItemLayer((ent, sprite, ent.Comp), comp, rotation))
                return;
        }

        foreach (var spriteLayer in itemSprite.AllLayers)
        {
            TryAddSpriteLayer((ent, sprite, ent.Comp), spriteLayer, rotation);
        }
    }

    private bool TryAddSpriteLayer(Entity<SpriteComponent, EldritchPaperComponent> ent, ISpriteLayer spriteLayer, Angle rotation)
    {
        if (spriteLayer is not SpriteComponent.Layer layer || !_sprite.IsVisible(layer))
            return false;

        int? index = layer.ActualRsi is { } rsi && layer.ActualState?.StateId is { } state
            ? _sprite.AddRsiLayer(ent.AsNullable(), state, rsi)
            : layer.Texture is { } tex
                ? _sprite.AddTextureLayer(ent.AsNullable(), tex)
                : null;

        if (index is not { } i)
            return false;

        FinalizeLayer(ent, i, rotation);
        return true;
    }

    private bool TryAddStoredItemLayer(Entity<SpriteComponent, EldritchPaperComponent> ent, ItemComponent comp, Angle rotation)
    {
        if (comp.StoredSprite is not { } storedSprite)
            return false;

        int? index = storedSprite is SpriteSpecifier.Rsi rsi
            ? _sprite.AddRsiLayer(ent.AsNullable(), rsi.RsiState, rsi.RsiPath)
            : storedSprite is SpriteSpecifier.Texture texture
                ? _sprite.AddTextureLayer(ent.AsNullable(), _sprite.GetTexture(texture))
                : null;

        if (index is not { } i)
            return false;

        FinalizeLayer(ent, i, rotation);
        return true;
    }

    private void FinalizeLayer(Entity<SpriteComponent, EldritchPaperComponent> ent, int i, Angle rotation)
    {
        var nullableEnt = ent.AsNullable();
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
