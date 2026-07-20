// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Projectiles;

/// <summary>
/// Event raised on a spread ammo entity for every pellet it spawns, when being shot.
/// </summary>
[ByRefEvent]
public record struct SpreadPelletFiredEvent(EntityUid Pellet);
