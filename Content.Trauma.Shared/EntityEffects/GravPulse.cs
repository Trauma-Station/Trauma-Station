// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects.Effects;

/// <summary>
/// Gravity well effect on target entity. (repulse / attract other ent's)
/// </summary>
public sealed partial class GravPulse : EntityEffectBase<GravPulse>
{
    /// <summary>
    /// The maximum range at which the gravity well can push/pull entities.
    /// </summary>
    [DataField]
    public float MaxRange;

    /// <summary>
    /// The minimum range at which the gravity well can push/pull entities.
    /// This is effectively hardfloored at <see cref="GravityWellSystem.MinGravPulseRange"/>.
    /// </summary>
    [DataField]
    public float MinRange = 0f;

    /// <summary>
    /// The acceleration entities will experience towards the gravity well at a distance of 1m.
    /// Negative values accelerate entities away from the gravity well.
    /// Actual acceleration scales with the inverse of the distance to the entity.
    /// </summary>
    [DataField]
    public float BaseRadialAcceleration = 0.0f;

    /// <summary>
    /// The acceleration entities will experience tangent to the gravity well at a distance of 1m.
    /// Positive tangential acceleration is counter-clockwise.
    /// Actual acceleration scales with the inverse of the distance to the entity.
    /// </summary>
    [DataField]
    public float BaseTangentialAcceleration = 0.0f;
}