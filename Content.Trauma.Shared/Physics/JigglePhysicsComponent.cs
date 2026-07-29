// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Physics;

/// <summary>
/// Gives an entity displacement based jiggle physics, which are simulated clientside with a spring model.
/// This component just stores the model configuration, the rest is in <c>JigglePhysicsVisualsComponent</c>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class JigglePhysicsComponent : Component
{
    /// <summary>
    /// Mass of what is jiggling, makes it harder to accelerate.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public float Mass;

    /// <summary>
    /// Spring constant for returning <c>Jiggle<c> to 0.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public float Springiness;

    /// <summary>
    /// Limit on the absolute value of <c>Jiggle</c>, it can be negative so this is both the lower and upper bound.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public float JiggleLimit;

    /// <summary>
    /// RSI to get displacements from.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ResPath DisplacementsRsi = default!;

    /// <summary>
    /// Prefix for RSI states.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string DisplacementPrefix = "jiggle_";

    /// <summary>
    /// Number of displacements there are for positive and negative momentum each.
    /// Higher number means higher fidelity as momentum changes
    /// </summary>
    /// <example>
    /// For count of 3, the states end with 1, 2, 3, -1, -2, -3
    /// </example>
    [DataField(required: true), AutoNetworkedField]
    public int DisplacementCount;

    /// <summary>
    /// The sprite layer to apply displacements to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Enum LayerKey = JigglePhysicsVisuals.Layer;
}

[Serializable, NetSerializable]
public enum JigglePhysicsVisuals : byte
{
    Layer
}
