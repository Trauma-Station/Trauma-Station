using Robust.Shared.Map;

namespace Content.Server.SurveillanceCamera;

public sealed partial class SurveillanceCameraMonitorComponent
{
    /// <summary>
    /// The same as KnownCameras but for MobileCameras only: sec bodycams, no pro, dragable wireless camera
    /// </summary>
    [ViewVariables]
    public Dictionary<string, (string, (NetEntity, NetCoordinates))> KnownMobileCameras = new();

    /// <summary>
    /// Mobile cameras should receive a heartbeat as they constantly stream their location
    /// </summary>
    [ViewVariables]
    public Dictionary<string, float> KnownMobileCamerasLastHeartbeat = new();

    /// <summary>
    /// Mobile cameras should receive a heartbeat as they constantly stream their location
    /// </summary>
    [ViewVariables]
    public Dictionary<string, float> KnownMobileCamerasLastHeartbeatSent = new();
}
