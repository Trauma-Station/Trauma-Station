namespace Content.Trauma.Common.Bulletholes;

/// <summary>
/// Raised on the entity that got hit by a projectile
/// </summary>
[ByRefEvent]
public readonly record struct GotHitByProjectileEvent(EntityUid Projectile);
