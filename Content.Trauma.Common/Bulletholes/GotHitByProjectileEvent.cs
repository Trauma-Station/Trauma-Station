// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Bulletholes;

/// <summary>
/// Raised on the entity that got hit by a projectile.
/// </summary>
[ByRefEvent]
public readonly record struct GotHitByProjectileEvent(EntityUid Projectile, bool Cancel = false);
