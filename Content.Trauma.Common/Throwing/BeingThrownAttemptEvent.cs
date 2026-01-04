// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Trauma.Common.Throwing;

/// <summary>
/// Raised on an entity that is about to be thrown to allow preventing it.
/// </summary>
[ByRefEvent]
public record struct BeingThrownAttemptEvent(bool Cancelled = false);
