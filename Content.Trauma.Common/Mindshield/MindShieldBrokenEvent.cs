// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Mindshield;

/// <summary>
/// Raised on a mob when it gets mindshielded but it was broken by <see cref="MindShieldAttemptEvent"/>.
/// </summary>
[ByRefEvent]
public record struct MindShieldBrokenEvent();
