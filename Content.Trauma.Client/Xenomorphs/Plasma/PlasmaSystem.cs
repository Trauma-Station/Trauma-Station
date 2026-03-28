// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Alerts;
using Content.Trauma.Shared.Xenomorphs.Plasma;
using Content.Trauma.Shared.Xenomorphs.Plasma.Components;

namespace Content.Trauma.Client.Xenomorphs.Plasma;

public sealed class PlasmaSystem : SharedPlasmaSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlasmaVesselComponent, UpdateAlertSpriteEvent>(OnUpdateAlertSprite);
    }

    private void OnUpdateAlertSprite(EntityUid uid, PlasmaVesselComponent component, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.ID != component.PlasmaAlert)
            return;

        var sprite = args.SpriteViewEnt.Comp;
        var plasma = Math.Clamp(component.Plasma.Int(), 0, 999);

        sprite.LayerSetState(PlasmaVisualLayers.Digit1, $"{plasma / 100 % 10}");
        sprite.LayerSetState(PlasmaVisualLayers.Digit2, $"{plasma / 10 % 10}");
        sprite.LayerSetState(PlasmaVisualLayers.Digit3, $"{plasma % 10}");
    }
}
