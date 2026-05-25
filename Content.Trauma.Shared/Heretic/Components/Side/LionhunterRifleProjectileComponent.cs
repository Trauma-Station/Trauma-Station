// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Heretic.Components.Side;

/// <summary>
/// Empowered projectile shot by <see cref="LionhunterRifleComponent"/>
/// Empowered means fully aimed by <see cref="AimedRifleComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LionhunterRifleProjectileComponent : Component
{
    /// <summary>
    /// Components to add to this if empowered
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry ComponentsOnEmpower;

    /// <summary>
    /// Damage multiplier if empowered
    /// </summary>
    [DataField]
    public float EmpowerDamageMultiplier = 2f;

    /// <summary>
    /// Knockdown time for target if empowered
    /// </summary>
    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// Current target that we have aimed at
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? EmpowerTarget;

    /// <summary>
    /// Shooter heretic path if applicable
    /// </summary>
    [DataField, AutoNetworkedField]
    public HereticPath? ShooterPath;

    /// <summary>
    /// Shooter heretic passive level
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ShooterPassiveLevel = 1;
}
