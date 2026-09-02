// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Trauma.Shared.Paint;

namespace Content.Trauma.Client.Paint;

/// <summary>
/// Colours layers of painted entities that aren't unshaded.
/// Also colours the spray can colour layers.
/// </summary>
public sealed partial class PaintVisualizerSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;

    [SubscribeLocalEvent]
    private void OnCanInit(Entity<PaintCanComponent> ent, ref ComponentInit args)
    {
        // get layer before hand, don't error if no layer is found
        if (_sprite.TryGetLayer(ent.Owner, PaintCanVisuals.Layer, out var layer, false))
            _sprite.LayerSetColor(layer, ent.Comp.Color);
    }

    [SubscribeLocalEvent]
    private void OnHandleState(Entity<PaintVisualsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        PaintSprite(ent);
    }

    [SubscribeLocalEvent]
    private void OnPainted(Entity<PaintVisualsComponent> ent, ref PaintedEvent args)
    {
        PaintSprite(ent);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<PaintVisualsComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent) || !_spriteQuery.TryComp(ent, out var sprite))
            return;

        var spriteEnt = new Entity<SpriteComponent?>(ent, sprite);
        foreach (var (key, color) in ent.Comp.LayerColors)
        {
            _sprite.LayerSetColor(spriteEnt, key, color);
        }
    }

    [SubscribeLocalEvent]
    private void OnHeldVisualsUpdated(Entity<PaintVisualsComponent> ent, ref HeldVisualsUpdatedEvent args)
    {
        SetLayers(args.User, ent.Comp.Color, args.RevealedLayers);
    }

    [SubscribeLocalEvent]
    private void OnEquipmentVisualsUpdated(Entity<PaintVisualsComponent> ent, ref EquipmentVisualsUpdatedEvent args)
    {
        SetLayers(args.Equipee, ent.Comp.Color, args.RevealedLayers);
    }

    private void PaintSprite(Entity<PaintVisualsComponent> ent)
    {
        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        var colors = ent.Comp.LayerColors;
        colors.Clear();
        int index = 0; // god sprite api is so shit
        foreach (var iLayer in sprite.AllLayers)
        {
            int i = index++;
            // don't replace layers that are unshaded (lights etc)
            if (iLayer is not SpriteComponent.Layer layer ||
                layer.ShaderPrototype == SpriteSystem.UnshadedId)
                continue;

            colors[i] = layer.Color;
            _sprite.LayerSetColor(layer, ent.Comp.Color);
        }
    }

    private void SetLayers(Entity<SpriteComponent?> ent, Color color, HashSet<string> keys)
    {
        if (!_spriteQuery.Resolve(ent, ref ent.Comp))
            return;

        var sprite = ent.Comp;
        var spriteEnt = new Entity<SpriteComponent?>(ent, sprite);
        foreach (var key in keys)
        {
            if (!_sprite.LayerMapTryGet(spriteEnt, key, out var index, true))
                continue;

            _sprite.LayerSetColor(spriteEnt, index, color);
        }
    }
}
