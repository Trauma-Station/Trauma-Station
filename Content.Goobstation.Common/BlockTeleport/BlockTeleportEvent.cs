// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Common.BlockTeleport;

[ByRefEvent]
public record struct TeleportAttemptEvent(
    bool Predicted = true,
    string? Message = "teleport-blocked-message",
    bool Cancelled = false);
