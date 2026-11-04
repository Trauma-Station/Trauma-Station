// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Hands;

/// <summary>
/// Raised on an item before it is thrown, can be used to cancel the throw
/// </summary>
[ByRefEvent]
public record struct BeforeGettingThrownEvent(EntityUid User, bool Cancelled = false);
