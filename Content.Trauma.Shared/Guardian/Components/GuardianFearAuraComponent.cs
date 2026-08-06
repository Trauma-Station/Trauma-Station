using Content.Shared.Prototypes;

namespace Content.Trauma.Shared.Guardian.Components;

/// <summary>
/// Passive fear aura that significantly slows nearby enemies of a manifested guardian.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GuardianFearAuraComponent : Component
{
    /// <summary>
    /// Radius in tiles around the guardian in which enemies are slowed.
    /// </summary>
    [DataField]
    public float Radius = 2f;

    /// <summary>
    /// Multiplier applied to the walking speed of affected enemies.
    /// </summary>
    [DataField]
    public float WalkSpeedModifier = 0.5f;

    /// <summary>
    /// Multiplier applied to the sprinting speed of affected enemies.
    /// </summary>
    [DataField]
    public float SprintSpeedModifier = 0.5f;

    /// <summary>
    /// Slowdown status effect prototype applied to affected enemies.
    /// </summary>
    [DataField]
    public EntProtoId SlowdownEffect = "GuardianFearSlowdownStatusEffect";

    /// <summary>
    /// How long the slowdown lingers after an enemy leaves the aura.
    /// </summary>
    [DataField]
    public TimeSpan SlowdownDuration = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How often the aura re-checks for nearby enemies.
    /// </summary>
    [DataField]
    public TimeSpan CheckInterval = TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan NextCheck;
}
