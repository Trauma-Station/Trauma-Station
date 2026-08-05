// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Client.Physics;

/// <summary>
/// Stores simulation and sprite data for <c>JigglePhysicsComponent</c>, gets added and reoved when it does.
/// </summary>
[RegisterComponent, Access(typeof(JigglePhysicsSystem))]
public sealed partial class JigglePhysicsVisualsComponent : Component
{
    /// <summary>
    /// Current clientside "position", changed over time by <see cref="Slap"/>.
    /// </summary>
    [ViewVariables]
    public float Jiggle;

    /// <summary>
    /// Velocity used to change <see cref="Jiggle"/>.
    /// </summary>
    [ViewVariables]
    public float Slap;

    /// <summary>
    /// The last frame's <c>PhysicsComponent.LinearVelocity</c> used to estimate acceleration.
    /// </summary>
    [ViewVariables]
    public Vector2 LastParentVelocity;

    /// <summary>
    /// The current displacement number that should be used.
    /// 0 means no displacement is applied.
    /// </summary>
    [ViewVariables]
    public int DisplacementNumber;

    /// <summary>
    /// The displacement layers added.
    /// </summary>
    [ViewVariables]
    public List<SpriteComponent.Layer> Layers = new();
}
