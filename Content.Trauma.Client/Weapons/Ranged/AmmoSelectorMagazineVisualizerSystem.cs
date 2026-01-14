using Content.Client.Weapons.Ranged.Components;
using Content.Shared._Goobstation.Weapons.AmmoSelector;
using Content.Shared.Rounding;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Trauma.Client.Weapons.Ranged;

public sealed class AmmoSelectorMagazineVisualizerSystem : VisualizerSystem<AmmoSelectorMagazineVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, AmmoSelectorMagazineVisualsComponent component, ref AppearanceChangeEvent args)
    {
        args.AppearanceData.TryGetValue(AmmoVisuals.AmmoCount, out var count);
        count ??= 0;
        args.AppearanceData.TryGetValue(AmmoVisuals.AmmoMax, out var capacity);
        capacity ??= int.MaxValue;
        var step = ContentHelpers.RoundToLevels((int)count, (int)capacity, component.MagSteps);

        if (!args.AppearanceData.TryGetValue(AmmoSelectorVisuals.Selected, out var selection))
        {
            DebugTools.Assert($"Gun ({uid}) does not have AmmoSelectorVisuals.Selected appearance data set. Does it have the AmmoSelectorComponent?");
            return;
        }

        if (!component.MagStates.TryGetValue((string) selection, out var state))
        {
            DebugTools.Assert($"Gun ({uid}) cannot handle ammo selection {selection}.");
            return;
        }

        // have to check if the layers exist because otherwise it will throw error logs
        // we handle both mag and mag unshaded layers because a gun could have a magazine with unshaded glowy bits and both layers would coexist
        var mag = SpriteSystem.LayerMapTryGet(uid, GunVisualLayers.Mag, out _, false);
        var magUnshaded = SpriteSystem.LayerMapTryGet(uid, GunVisualLayers.MagUnshaded, out _, false);

        if (step == 0 && !component.ZeroVisible)
        {
            if (mag)
                SpriteSystem.LayerSetVisible(uid, GunVisualLayers.Mag, false);
            if (magUnshaded)
                SpriteSystem.LayerSetVisible(uid, GunVisualLayers.MagUnshaded, false);
            return;
        }

        var fullStateMag = $"{state}-{step}";
        var fullStateMagUnshaded = $"{state}-unshaded-{step}";

        if (mag)
        {
            SpriteSystem.LayerSetVisible(uid, GunVisualLayers.Mag, true);
            SpriteSystem.LayerSetRsiState(uid, GunVisualLayers.Mag, fullStateMag);
        }

        if (magUnshaded)
        {
            SpriteSystem.LayerSetVisible(uid, GunVisualLayers.MagUnshaded, true);
            SpriteSystem.LayerSetRsiState(uid, GunVisualLayers.MagUnshaded, fullStateMagUnshaded);
        }
    }
}
