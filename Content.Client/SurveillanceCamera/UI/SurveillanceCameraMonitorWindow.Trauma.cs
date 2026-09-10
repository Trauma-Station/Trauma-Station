using Content.Client.Pinpointer.UI;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Client.SurveillanceCamera.UI;

public sealed partial class SurveillanceCameraMonitorWindow
{
    private SpriteSystem _sprite = default!;
    private readonly Dictionary<NetEntity, string> _reverseCameras = new();
    private readonly Dictionary<string, string> _resolveCameraName = new();
    private Texture? _blipTexture;

    private void InitTrauma()
    {
        _sprite = _entityManager.System<SpriteSystem>();
        NavMap.TrackedEntitySelectedAction += SetTrackedEntityFromNavMap;
        SubnetRefreshButton.OnPressed += _ => SubnetRefresh?.Invoke();
        CameraRefreshButton.OnPressed += _ => CameraRefresh?.Invoke();
        CameraDisconnectButton.OnPressed += _ => CameraDisconnect?.Invoke();
    }

    // need to translate entity to string and then call the same method the list does
    private void SetTrackedEntityFromNavMap(NetEntity? netEntity)
    {
        if (netEntity is not { } camera)
            return;

        CameraSelected?.Invoke(_reverseCameras[camera], null);
    }

    public EntityUid Entity;

    // Needed for NavMap to initialize and draw the grid
    public void SetEntity(EntityUid uid)
    {
        Entity = uid;

        // Pass owner to nav map
        NavMap.Owner = uid;

        // Set nav map grid uid
        var stationName = Loc.GetString("surveillance-camera-monitor-ui-unknown-location");

        if (_entityManager.TryGetComponent<TransformComponent>(uid, out var xform))
        {
            NavMap.MapUid = xform.GridUid;

            // Assign station name
            if (_entityManager.TryGetComponent<MetaDataComponent>(xform.GridUid, out var stationMetaData))
                stationName = stationMetaData.EntityName;

            var msg = new FormattedMessage();
            msg.AddMarkupOrThrow(Loc.GetString("surveillance-camera-monitor-ui-station-name", ("stationName", stationName)));

            StationName.SetMessage(msg);
            _blipTexture = _sprite.Frame0(new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/NavMap/beveled_circle.png")));
        }

        else
        {
            StationName.SetMessage(stationName);
            NavMap.Visible = false;
        }
    }

    // Add a particular camera
    private void AddTrackedEntityToNavMap(NetEntity ent, NetCoordinates coordinates, bool selected, bool mobile)
    {
        var coords = _entityManager.GetCoordinates(coordinates);
        var texture = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/NavMap/beveled_square.png"));
        var color = selected ? Color.Green : Color.Red;
        var blink = false;
        var modulator = Color.White;

        if (mobile)
            color = selected ? Color.Green : Color.Orange;
        else
            color = selected ? Color.Green : Color.Red;

        var blip = new NavMapBlip(coords, _sprite.Frame0(texture), color * modulator, blink);
        NavMap.TrackedEntities[ent] = blip;
    }
}
