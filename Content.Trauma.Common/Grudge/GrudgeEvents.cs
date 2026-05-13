// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Grudge;


/// <summary>
/// Raised on a grudge. Passes owner and grudgee for informational setup.
/// </summary>
[ByRefEvent]
public record struct GrudgeAddedEvent(EntityUid Accuser, EntityUid Accused, EntityUid AccuserObjective, EntityUid AccusedObjective);

/// <summary>
/// Raised on a grudge. Setups data.
/// </summary>
[ByRefEvent]
public record struct GrudgeUpdateEvent();
