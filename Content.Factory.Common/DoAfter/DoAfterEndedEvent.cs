// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Factory.Common.DoAfter;

/// <summary>
/// Event raised on the doafter's user after a doafter ends.
/// </summary>
[ByRefEvent]
public readonly record struct DoAfterEndedEvent(EntityUid? Target, bool Cancelled);
