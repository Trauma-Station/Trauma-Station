// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Armor;

/// <summary>
/// Event raised on armor when trying to protect from damage, potentially coming from a source entity.
/// </summary>
[ByRefEvent]
public record struct ArmorProtectAttemptEvent(EntityUid? Origin, bool Cancelled = false);
