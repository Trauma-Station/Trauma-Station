// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Item;
using Content.Trauma.Shared.Forging;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Trauma.Client.Forging;

public sealed partial class MetalSystem : SharedMetalSystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private EntityQuery<ItemComponent> _itemQuery = default!;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;

    public static readonly ProtoId<ShaderPrototype> EmissiveShader = "Emissive";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MetallicComponent, ComponentStartup>(OnMetalStartup);
        SubscribeLocalEvent<SpriteComponent, MetalChangedEvent>(OnSpriteChanged);
    }

    private void OnMetalStartup(Entity<MetallicComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.Metal is {} metal)
            UpdateSprites(ent.Owner, Proto.Index(metal));
    }

    private void OnSpriteChanged(Entity<SpriteComponent> ent, ref MetalChangedEvent args)
    {
        UpdateSprites(ent.AsNullable(), args.Metal);
    }

    private void UpdateSprites(Entity<SpriteComponent?> ent, MetalPrototype proto)
    {
        if (!_spriteQuery.Resolve(ent, ref ent.Comp))
            return;

        var color = proto.Color;
        if (_sprite.LayerMapTryGet(ent, MetallicVisuals.Layer, out var index, false))
            _sprite.LayerSetColor(ent, index, color);

        if (!_itemQuery.TryComp(ent, out var item))
            return;

        var visuals = item.InhandVisuals;
        foreach (var layers in visuals.Values)
        {
            foreach (var layer in layers)
            {
                // no api for checking layer for a PrototypeLayerData so this is good enough
                if (layer.Shader == EmissiveShader.Id)
                    layer.Color = color;
            }
        }
    }
}
