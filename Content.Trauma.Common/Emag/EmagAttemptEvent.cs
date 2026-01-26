// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Trauma.Common.Emag;

/// <summary>
/// Event raised on the emag to allow preventing it from working.
/// </summary>
[ByRefEvent]
public record struct EmagAttemptEvent(EntityUid Target, EntityUid User, bool Cancelled = false);
