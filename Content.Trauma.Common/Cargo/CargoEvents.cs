// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Cargo;

/// <summary>
/// Raised on a station entity to modify the reward for a bounty.
/// </summary>
[ByRefEvent]
public record struct ModifyBountyRewardEvent(int Reward);

/// <summary>
/// Raised on a station entity to modify the price of a product.
/// </summary>
[ByRefEvent]
public record struct ModifyCargoPriceEvent(int Price);
