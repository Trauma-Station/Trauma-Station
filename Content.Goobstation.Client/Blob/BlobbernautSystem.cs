// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.DamageState;
using Content.Goobstation.Shared.Blob;
using Content.Goobstation.Shared.Blob.Components;

namespace Content.Goobstation.Client.Blob;

public sealed partial class BlobbernautSystem : SharedBlobbernautSystem;

public sealed partial class BlobbernautVisualizerSystem : VisualizerSystem<BlobbernautComponent>
{
    [Dependency] private SpriteSystem _sprite = default!;

    private static readonly DamageStateVisualLayers[] Layers =
    [
        DamageStateVisualLayers.Base, DamageStateVisualLayers.BaseUnshaded,
    ];

    private void UpdateAppearance(Entity<BlobbernautComponent> ent, SpriteComponent? sprite = null)
    {
        if (!Resolve(ent, ref sprite))
            return;

        var color = ProtoMan.Index(ent.Comp.CurrentChem).Color;
        foreach (var key in Layers)
        {
            _sprite.LayerSetColor((ent, sprite), key, color);
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, BlobbernautComponent comp, ref AppearanceChangeEvent args)
    {
        UpdateAppearance((uid, comp), args.Sprite);
    }

    [SubscribeLocalEvent]
    private void OnBlobTileHandleState(Entity<BlobbernautComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateAppearance(ent);
    }
}
