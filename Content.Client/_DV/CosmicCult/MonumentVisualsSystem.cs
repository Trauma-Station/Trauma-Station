using Robust.Client.GameObjects;
using Content.Shared._DV.CosmicCult.Components;

namespace Content.Client._DV.CosmicCult;

/// <summary>
/// Visualizer for The Monument of the Cosmic Cult.
/// </summary>
public sealed class MonumentVisualizerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MonumentComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnAppearanceChanged(Entity<MonumentComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        args.Sprite.LayerMapTryGet(MonumentVisualLayers.TransformLayer, out var transformLayer);
        args.Sprite.LayerMapTryGet(MonumentVisualLayers.FinaleLayer, out var finaleLayer);
        args.Sprite.LayerMapTryGet(MonumentVisualLayers.MonumentLayer, out var baseLayer);
        _appearance.TryGetData<bool>(ent, MonumentVisuals.Transforming, out var transforming, args.Component);

        if (transforming && HasComp<MonumentTransformingComponent>(ent))
        {
            args.Sprite.LayerSetAnimationTime(transformLayer, 0f);
            args.Sprite.LayerSetVisible(transformLayer, true);
            args.Sprite.LayerSetVisible(finaleLayer, false);
            args.Sprite.LayerSetVisible(baseLayer, false);
        }
        else
        {
            args.Sprite.LayerSetVisible(transformLayer, false);
            args.Sprite.LayerSetVisible(finaleLayer, true);
            args.Sprite.LayerSetVisible(baseLayer, true);
        }
    }
}
