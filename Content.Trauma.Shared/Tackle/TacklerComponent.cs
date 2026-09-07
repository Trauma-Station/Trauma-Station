// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Tackle;

/// <summary>
/// This component doesn't make user tackle by default, they still need special equipment.
/// Unless they also have <see cref="TackleModifierComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class TacklerComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextTackle;

    [DataField]
    public TimeSpan TackleCooldown = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(1);

    [DataField]
    public float Range = 5f;

    [DataField]
    public float Speed = 10f;

    [DataField]
    public float MinDistance;

    [DataField]
    public float StaminaCost = 25f;

    /// <summary>
    /// Base damage when hitting a wall, multiplied by severity that is dependent on velocity
    /// </summary>
    [DataField]
    public DamageSpecifier BaseUserDamage = new()
    {
        DamageDict =
        {
            { "Blunt", 20 },
        },
    };

    /// <summary>
    /// Base time the user will be knocked on tackle collision
    /// </summary>
    [DataField]
    public float BaseUserKnockdownTime = 1f;

    /// <summary>
    /// Base stamina damage target will receive on collision
    /// </summary>
    [DataField]
    public float BaseTargetStaminaDamage = 22f;

    /// <summary>
    /// Base knockdown time of target during collision
    /// </summary>
    [DataField]
    public float BaseTargetKnockdownTime = 2f;
}
