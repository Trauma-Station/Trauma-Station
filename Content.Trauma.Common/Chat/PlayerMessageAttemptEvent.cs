// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Player;

namespace Content.Trauma.Common.Chat;

/// <summary>
/// Event broadcast to prevent a player making a message.
/// </summary>
[ByRefEvent]
public record struct PlayerMessageAttemptEvent(ICommonSession Session, string Message, bool Cancelled = false);
