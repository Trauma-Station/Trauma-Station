// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Lavaland.Shared.Megafauna.Events;

/// <summary>
/// Raised when boss is fully defeated.
/// </summary>
[ByRefEvent]
public record struct MegafaunaKilledEvent;

/// <summary>
/// Raised when MegafaunaAi becomes active and starts calculating logic
/// </summary>
[ByRefEvent]
public record struct MegafaunaStartupEvent;

/// <summary>
/// Raised when boss doesn't die but for any reason deactivates.
/// </summary>
[ByRefEvent]
public record struct MegafaunaShutdownEvent;
