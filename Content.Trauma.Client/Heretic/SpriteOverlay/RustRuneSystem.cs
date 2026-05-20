// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Tag;
using Content.Trauma.Common.Heretic;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Rust;
using Robust.Client.GameObjects;
using Robust.Shared.Random;

namespace Content.Trauma.Client.Heretic.SpriteOverlay;

public sealed partial class RustRuneSystem : SpriteOverlaySystem<RustRuneComponent>
{
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RustRuneComponent, IconSmoothCornersInitializedEvent>(OnIconSmoothInit);

        SubscribeLocalEvent<SpriteRandomOffsetComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<SpriteRandomOffsetComponent> ent, ref ComponentStartup args)
    {
        var (uid, comp) = ent;

        Sprite.SetOffset(uid, _random.NextVector2Box(comp.MinX, comp.MinY, comp.MaxX, comp.MaxY));
    }

    private void OnIconSmoothInit(Entity<RustRuneComponent> ent, ref IconSmoothCornersInitializedEvent args)
    {
        RemoveOverlay(ent.Owner, ent.Comp);
        AddOverlay(ent.Owner, ent.Comp);
    }

    protected override void UpdateOverlayLayer(Entity<SpriteComponent> ent,
        RustRuneComponent comp,
        int layer,
        EntityUid? source = null)
    {
        base.UpdateOverlayLayer(ent, comp, layer, source);

        var rune = comp.SelectedRune ?? _random.Pick(comp.RuneStates);
        comp.SelectedRune = rune;
        var diagonal = _tag.HasTag(ent, comp.DiagonalTag);
        var offset = comp.SelectedOffset ?? (diagonal ? comp.DiagonalOffset : _random.NextVector2Box(0.25f, 0.25f));
        comp.SelectedOffset = offset;

        Sprite.LayerSetRsiState(ent.AsNullable(), layer, rune);
        Sprite.LayerSetOffset(ent.AsNullable(), layer, offset);

        if (Sprite.TryGetLayer(ent.AsNullable(), layer, out var spriteLayer, true))
            spriteLayer.Loop = false;
    }
}
