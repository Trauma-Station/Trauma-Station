// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Dragon;

/// <summary>
/// Event raised on the dragon when it tries to spawn a rift.
/// Popup is shown to the user if cancelled and it's non-null.
/// </summary>
[ByRefEvent]
public record struct DragonSpawnRiftAttemptEvent(bool Cancelled = false, string? Popup = null);
