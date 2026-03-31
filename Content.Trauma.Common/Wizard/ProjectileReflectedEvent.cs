namespace Content.Trauma.Common.Wizard;

/// <summary>
/// Called on the projectile whenever it is reflected.
/// </summary>
[ByRefEvent]
public record struct ProjectileReflectedEvent(EntityUid Reflector, EntityUid User);
