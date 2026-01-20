// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Lavaland.Common.Weapons.Ranged;

/// <summary>
/// Raised on a gun when a projectile has been fired by it.
/// </summary>
[ByRefEvent]
public record struct ProjectileShotEvent(EntityUid FiredProjectile);
