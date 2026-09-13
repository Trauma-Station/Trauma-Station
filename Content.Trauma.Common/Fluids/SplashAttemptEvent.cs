// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Fluids;

/// <summary>
/// Raised on an entity splashing its solution on nearby reactive entities.
/// </summary>
[ByRefEvent]
public record struct SplashAttemptEvent(EntityUid Target, bool Cancelled = false);
