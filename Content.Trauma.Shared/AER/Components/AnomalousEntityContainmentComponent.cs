// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.AER;

/// <summary>
/// identifies machines that analyze anomalous entities
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AnomalousEntityContainmentComponent : Component
{
    /// <summary>
    /// The anomalous entity that the containment sensor is monitoring.
    /// Can be null.
    /// </summary>
    [DataField]
    public EntityUid? AnomalousEntity;

    /// <summary>
    /// if the aer sensor is linked to an aer
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Linked;

    /// <summary>
    /// A multiplier applied to the amount of points generated.
    /// </summary>
    [DataField]
    public float PointMultiplier = 1;

    /// <summary>
    /// Range of the containment sensor
    /// </summary>
    [DataField]
    public float Range = 1.6f;
}
