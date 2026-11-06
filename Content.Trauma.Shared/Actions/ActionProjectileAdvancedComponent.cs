// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Projectiles;

namespace Content.Trauma.Shared.Actions;

/// <summary>
/// Action component that shoots out advanced projectile patterns towards it's target
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActionProjectileAdvancedComponent : Component
{
    /// <summary>
    /// The patterns to shoot out when performing this action, towards the target
    /// </summary>
    [DataField(required: true)]
    public ProjectileAdvancedPattern[] ProjectilePatterns;
}

/// <summary>
/// Active component of <see cref="ActionProjectileAdvancedComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class ActiveActionProjectileAdvancedComponent : Component
{
    /// <summary>
    /// The index of the current pattern
    /// </summary>
    [AutoNetworkedField]
    public int PatternIdx;

    /// <summary>
    /// The delay to the next pattern
    /// </summary>
    [DataField, AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextPattern;

    /// <summary>
    /// The target of the action
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Target;

    /// <summary>
    /// The user of the action
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid User;
}

/// <summary>
/// A record that holds data about advanced projectile patterns
/// </summary>
/// <param name="ProjProto">The projectile prototype to shoot</param>
/// <param name="Angle">The full angle the projectiles will follow (e.g. 360 will make them do a 360 around us)</param>
/// <param name="Amount">How many projectiles to shoot</param>
/// <param name="Speed">The speed of the projectiles</param>
/// <param name="Delay">How long to wait for the next pattern</param>
[DataRecord]
public partial record struct ProjectileAdvancedPattern(
    EntProtoId<ProjectileComponent> ProjProto,
    int Angle,
    int Amount,
    float Speed,
    TimeSpan Delay);

public sealed partial class ActionProjectileAdvancedEvent : EntityTargetActionEvent;
