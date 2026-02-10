// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Lavaland.Common.Shuttles;

/// <summary>
/// Event broadcast to refresh shuttle consoles when this shuttle FTL state changed.
/// </summary>
[ByRefEvent]
public record struct RefreshShuttleConsolesEvent(EntityUid Grid);
