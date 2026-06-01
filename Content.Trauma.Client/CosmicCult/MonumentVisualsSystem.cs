// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.CosmicCult.Components;

namespace Content.Trauma.Client.CosmicCult;

/// <summary>
/// Visualizer for The Monument of the Cosmic Cult.
/// </summary>
public sealed partial class MonumentVisualizerSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MonumentComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnAppearanceChanged(Entity<MonumentComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } spriteComp)
            return;

        var sprite = new Entity<SpriteComponent?>(ent.Owner, spriteComp);
        var transformLayer = _sprite.LayerMapGet(sprite, MonumentVisualLayers.TransformLayer);
        var finaleLayer = _sprite.LayerMapGet(sprite, MonumentVisualLayers.FinaleLayer);
        var baseLayer = _sprite.LayerMapGet(sprite, MonumentVisualLayers.MonumentLayer);
        _appearance.TryGetData<bool>(ent, MonumentVisuals.Transforming, out var transforming, args.Component);

        transforming &= HasComp<MonumentTransformingComponent>(ent);

        if (transforming)
            _sprite.LayerSetAnimationTime(sprite, transformLayer, 0f);

        _sprite.LayerSetVisible(sprite, transformLayer, transforming);
        _sprite.LayerSetVisible(sprite, finaleLayer, !transforming);
        _sprite.LayerSetVisible(sprite, baseLayer, !transforming);
    }
}
