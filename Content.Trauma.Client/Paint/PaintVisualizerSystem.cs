// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Trauma.Shared.Paint;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Client.Paint;

/// <summary>
/// Colours layers of painted entities that don't have a shader set.
/// Also colours the spray can colour layers.
/// </summary>
public sealed class PaintVisualizerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private EntityQuery<SpriteComponent> _spriteQuery;

    public static readonly ProtoId<ShaderPrototype> ShaderId = "Greyscale";
    public ShaderInstance Shader = default!;

    public override void Initialize()
    {
        base.Initialize();

        _spriteQuery = GetEntityQuery<SpriteComponent>();

        Shader = _proto.Index(ShaderId).Instance();

        SubscribeLocalEvent<PaintCanComponent, ComponentInit>(OnCanInit);

        SubscribeLocalEvent<PaintVisualsComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<PaintVisualsComponent, PaintedEvent>(OnPainted);
        SubscribeLocalEvent<PaintVisualsComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PaintVisualsComponent, HeldVisualsUpdatedEvent>(OnHeldVisualsUpdated);
        SubscribeLocalEvent<PaintVisualsComponent, EquipmentVisualsUpdatedEvent>(OnEquipmentVisualsUpdated);
    }

    private void OnCanInit(Entity<PaintCanComponent> ent, ref ComponentInit args)
    {
        // get layer before hand, don't error if no layer is found
        if (_sprite.TryGetLayer(ent.Owner, PaintCanVisuals.Layer, out var layer, false))
            _sprite.LayerSetColor(layer, ent.Comp.Color);
    }

    private void OnHandleState(Entity<PaintVisualsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        PaintSprite(ent);
    }

    private void OnPainted(Entity<PaintVisualsComponent> ent, ref PaintedEvent args)
    {
        PaintSprite(ent);
    }

    private void OnShutdown(Entity<PaintVisualsComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent) || !_spriteQuery.TryComp(ent, out var sprite))
            return;

        var spriteEnt = new Entity<SpriteComponent?>(ent, sprite);
        foreach (var (key, color) in ent.Comp.LayerColors)
        {
            sprite.LayerSetShader(key, null, null);
            _sprite.LayerSetColor(spriteEnt, key, color);
        }
    }

    private void OnHeldVisualsUpdated(Entity<PaintVisualsComponent> ent, ref HeldVisualsUpdatedEvent args)
    {
        SetLayers(args.User, ent.Comp.Color, args.RevealedLayers);
    }

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
            // don't replace layers that already have a custom shader set
            if (iLayer is not SpriteComponent.Layer layer ||
                layer.ShaderPrototype == SpriteSystem.UnshadedId ||
                (layer.Shader != null && layer.Shader != Shader))
                continue;

            colors[i] = layer.Color;
            sprite.LayerSetShader(i, Shader, ShaderId);
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

            sprite.LayerSetShader(index, Shader, ShaderId);
            _sprite.LayerSetColor(spriteEnt, index, color);
        }
    }
}
