// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Mindshield;

/// <summary>
/// Raised on a mob when it gets mindshielded.
/// Cancel it by setting <c>CancelPopup</c> to prevent standard effects.
/// </summary>
[ByRefEvent]
public record struct MindShieldAttemptEvent(LocId? CancelPopup = null);
