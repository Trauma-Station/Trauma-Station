// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Alerts;
using Content.Trauma.Shared.Xenomorphs.Plasma;
using Content.Trauma.Shared.Xenomorphs.Plasma.Components;

namespace Content.Trauma.Client.Xenomorphs.Plasma;

public sealed partial class PlasmaSystem : SharedPlasmaSystem
{
    [Dependency] SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlasmaVesselComponent, UpdateAlertSpriteEvent>(OnUpdateAlertSprite);
    }

    // TODO: use GenericAlertCounter
    private void OnUpdateAlertSprite(EntityUid uid, PlasmaVesselComponent component, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.ID != component.PlasmaAlert)
            return;

        var plasma = Math.Clamp(component.Plasma.Int(), 0, 999);

        var sprite = args.SpriteViewEnt.AsNullable();
        _sprite.LayerSetRsiState(sprite, PlasmaVisualLayers.Digit1, $"{plasma / 100 % 10}");
        _sprite.LayerSetRsiState(sprite, PlasmaVisualLayers.Digit2, $"{plasma / 10 % 10}");
        _sprite.LayerSetRsiState(sprite, PlasmaVisualLayers.Digit3, $"{plasma % 10}");
    }
}
