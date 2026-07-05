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

    [DataField]
    public float Range = 1.6f;

    [ViewVariables]
    public EntProtoId? IDGear = "";
}
