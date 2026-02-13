// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <aviu00@protonmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client.IconSmoothing;
using Content.Shared._Goobstation.Heretic.Components;
using Content.Shared.Tag;
using Robust.Client.GameObjects;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Client._Shitcode.Heretic;

public sealed class RustRuneSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RustRuneComponent, ComponentStartup>(OnStartup, after: new[] { typeof(IconSmoothSystem) });
        SubscribeLocalEvent<RustRuneComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RustRuneComponent, IconSmoothCornersInitializedEvent>(OnIconSmoothInit);

        SubscribeLocalEvent<SpriteRandomOffsetComponent, ComponentStartup>(OnStartup);
    }


    private RustRuneComponent AddRustRune(EntityUid wall)
    {
        var rune = EnsureComp<RustRuneComponent>(wall);
        Dirty(wall, rune);

        return rune;
    }

    private void OnStartup(Entity<SpriteRandomOffsetComponent> ent, ref ComponentStartup args)
    {
        var (uid, comp) = ent;

        _spriteSystem.SetOffset(uid, _random.NextVector2Box(comp.MinX, comp.MinY, comp.MaxX, comp.MaxY));
    }

    private void OnIconSmoothInit(Entity<RustRuneComponent> ent, ref IconSmoothCornersInitializedEvent args)
    {
        var (uid, comp) = ent;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        RemoveLayers(sprite);
        AddLayers(uid, comp, sprite);
    }

    private void OnAfterAutoHandleState(Entity<RustRuneComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var (uid, comp) = ent;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        AddLayers(uid, comp, sprite);
    }

    private void OnShutdown(Entity<RustRuneComponent> ent, ref ComponentShutdown args)
    {
        var (uid, _) = ent;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        RemoveLayers(sprite);
    }

    private void OnStartup(Entity<RustRuneComponent> ent, ref ComponentStartup args)
    {
        var (uid, comp) = ent;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        AddLayers(uid, comp, sprite);
    }

    private void RemoveLayers(SpriteComponent sprite)
    {
        if (sprite.LayerMapTryGet(RustRuneKey.Rune, out var rune))
            sprite.RemoveLayer(rune);

        if (sprite.LayerMapTryGet(RustRuneKey.Overlay, out var overlay))
            sprite.RemoveLayer(overlay);
    }

    private void AddLayers(EntityUid uid, RustRuneComponent comp, SpriteComponent sprite)
    {
        var diagonal = _tag.HasTag(uid, comp.DiagonalTag);

        if (comp.RustOverlay && !sprite.LayerMapTryGet(RustRuneKey.Overlay, out _))
        {
            var layerIndex = sprite.AddLayer(diagonal ? comp.DiagonalSprite : comp.OverlaySprite);
            sprite.LayerMapSet(RustRuneKey.Overlay, layerIndex);
        }

        var rune = _random.Pick(comp.RuneSprites);

        if (!sprite.LayerMapTryGet(RustRuneKey.Rune, out var layer))
        {
            layer = sprite.AddLayer(rune);
            sprite.LayerMapSet(RustRuneKey.Rune, layer);
            sprite.LayerSetShader(RustRuneKey.Rune, "unshaded");
        }

        var offset = diagonal ? comp.DiagonalOffset : _random.NextVector2Box(0.25f, 0.25f);
        sprite.LayerSetOffset(layer, offset);

        if (_spriteSystem.TryGetLayer((uid, sprite), layer, out var spriteLayer, true))
            spriteLayer.Loop = false;
    }
}
