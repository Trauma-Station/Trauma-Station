// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.FixedPoint;

namespace Content.Shared.Projectiles;

/// <summary>
/// Trauma - extensions to projectile for damage and targeting changes.
/// </summary>
public sealed partial class ProjectileComponent
{
    /// <summary>
    /// When <see cref="IgnoreResistances"/> is false, only allow modifier events to increase damage.
    /// </summary>
    [DataField]
    public bool IncreaseOnly;

    [DataField]
    public bool Penetrate;

    [DataField]
    public List<EntityUid> IgnoredEntities = new();

    [DataField]
    public Vector2 TargetCoordinates;

    /// <summary>
    /// Original shooter, used for prediction purposes
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? OriginalShooter;

    /// <summary>
    ///     When a projectile has this threshold set, it will continue to penetrate entities until the damage dealt reaches this threshold.
    /// </summary>
    [DataField]
    public FixedPoint2 PenetrationThreshold = 10f;

    /// <summary>
    ///     If set, the projectile will not penetrate objects that lack the ability to take these damage types.
    /// </summary>
    [DataField]
    public List<string>? PenetrationDamageTypeRequirement;

    /// <summary>
    ///     Tracks the amount of damage dealt for penetration purposes.
    /// </summary>
    [DataField]
    public FixedPoint2 PenetrationAmount = FixedPoint2.Zero;
}
