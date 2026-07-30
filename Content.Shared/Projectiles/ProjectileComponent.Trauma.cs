// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;

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
}
