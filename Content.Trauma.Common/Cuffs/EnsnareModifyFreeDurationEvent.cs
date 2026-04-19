// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Common.Cuffs;

/// <summary>
/// Raised on an entity to see if anything modifies it ensnare duration time to get out.
/// </summary>
[ByRefEvent]
public record struct EnsnareModifyFreeDurationEvent(EntityUid Target, float FreeTime);
