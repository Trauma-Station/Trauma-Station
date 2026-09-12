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
    /// Spring constant for returning <c>Jiggle<c> to 0.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public float Springiness;

    /// <summary>
    /// Damping force used to bring <c>Slap</c> to 0.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public float Damping;

    /// <summary>
    /// Scale of the force proportional to the entity's actual acceleration.
    /// Not really related to mass sadly.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public float InertiaScale;

    /// <summary>
    /// Limit on the magnitute of the <c>Jiggle</c> vector.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public float JiggleLimit;

    /// <summary>
    /// Limit on the magnitute of the <c>Slap</c> vector.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public float SlapLimit;

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
    /// Number of displacements there are for positive and negative 1-D <c>Jiggle</c> each.
    /// Negative means west, positive means east.
    /// Zero jiggle always gets 1 displacement.
    /// Higher number means higher fidelity as <c>Jiggle</c> changes
    /// </summary>
    /// <example>
    /// For count of 3, the states end with 1, 2, 3, 0, -1, -2, -3
    /// </example>
    [DataField(required: true), AutoNetworkedField]
    public int DisplacementCount;

    /// <summary>
    /// All sprite layers to apply displacements to, if they exist.
    /// </summary>
    [DataField(required: true)]
    public List<string> Layers = default!;
}
