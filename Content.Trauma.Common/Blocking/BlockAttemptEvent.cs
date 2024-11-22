// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Blocking;

/// <summary>
/// Raised on a shield when the user is attacked.
/// </summary>
[ByRefEvent]
public record struct BlockAttemptEvent(EntityUid User, bool Cancelled = false);
