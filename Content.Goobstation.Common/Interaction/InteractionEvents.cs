// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameObjects;

namespace Content.Goobstation.Common.Interaction;

/// <summary>
///     UseAttempt, but for item.
/// </summary>
[ByRefEvent]
public record struct UseInHandAttemptEvent(EntityUid User, bool Cancelled = false);
