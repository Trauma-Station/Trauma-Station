// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Trauma.Common.Salvage;

/// <summary>
/// Raised on the lathe and broadcast when someone claims mining points
/// </summary>
[ByRefEvent]
public readonly record struct MiningPointsClaimedEvent(EntityUid User, int Points);
