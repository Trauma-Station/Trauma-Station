// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Lavaland.Common.Weapons.Marker;

/// <summary>
/// Raised on the weapon after attacking a damage-marker mob.
/// </summary>
[ByRefEvent]
public record struct ApplyMarkerBonusEvent(EntityUid Target, EntityUid User);
