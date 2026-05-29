// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Prying;

/// <summary>
/// Raised on the user after a prying sequence has succeeded.
/// </summary>
[ByRefEvent]
public record struct PriedSuccessEvent;

/// <summary>
/// Raised on the user before trying a prying sequence.
/// </summary>
[ByRefEvent]
public record struct PryAttemptEvent(EntityUid Target, bool Cancelled = false);
