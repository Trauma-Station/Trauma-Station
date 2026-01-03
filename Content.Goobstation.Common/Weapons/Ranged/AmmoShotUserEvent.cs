namespace Content.Goobstation.Common.Weapons.Ranged;

/// <summary>
/// Raised on a user when projectiles have been fired from gun.
/// </summary>
[ByRefEvent]
public record struct AmmoShotUserEvent(EntityUid Gun, List<EntityUid> FiredProjectiles);
