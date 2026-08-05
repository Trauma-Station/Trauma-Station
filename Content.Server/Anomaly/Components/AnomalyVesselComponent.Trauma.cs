namespace Content.Server.Anomaly.Components;

public sealed partial class AnomalyVesselComponent : Component
{
    /// <summary>
    /// Text of closest beacon of anomaly location
    /// </summary>
    [DataField]
    public string? BeaconLocation;
}
