// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Chat;

/// <summary>
/// Event broadcast to prevent a player making a message, using an entity for the player.
/// </summary>
[ByRefEvent]
public record struct UserMessageAttemptEvent(EntityUid User, string Message, bool Cancelled = false);
