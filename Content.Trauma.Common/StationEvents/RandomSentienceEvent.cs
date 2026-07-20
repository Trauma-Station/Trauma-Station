// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.StationEvents;

/// <summary>
/// Broadcast and raised on a mob when random sentience gamerule makes it a ghost role.
/// </summary>
[ByRefEvent]
public record struct RandomSentienceEvent(EntityUid Target);
