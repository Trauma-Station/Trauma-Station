// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Common.Wraith;

/// <summary>
/// Raised on an entity when a wraith tries to curse it.
/// </summary>
[ByRefEvent]
public record struct CurseAttemptEvent(EntityUid Curser, bool Cancelled = false);
