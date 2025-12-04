// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.Player;

namespace Content.Trauma.Common.ServerCurrency;

/// <summary>
/// Broadcast to modify a player's earned currency after the round ends.
/// </summary>
[ByRefEvent]
public record struct ModifyCurrencyEvent(ICommonSession Session, int Money);
