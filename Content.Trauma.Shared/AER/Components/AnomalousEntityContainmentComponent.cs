namespace Content.Trauma.Shared.AER;

/// <summary>
/// identifies machines that analyze anomalous entities
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AnomalousEntityContainmentComponent : Component
{
    /// <summary>
    /// The anomalous entity that the containment sensor is monitoring.
    /// Can be null.
    /// </summary>
    [ViewVariables]
    public EntityUid? AnomalousEntity;
    /// <summary>
    /// A multiplier applied to the amount of points generated.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float PointMultiplier = 1;
    /// <summary>
    /// Range of the containment sensor
    /// </summary>
    [DataField]
    public float Range = 1.6f;
    /// <summary>
    /// Currently assigned I.D. gear to spawn on behaviours
    /// </summary>
    [ViewVariables]
    public EntProtoId? IDGear = "";
}
