// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Trauma.Common.Kitchen;

/// <summary>
/// Raised on the user after cooking some food via microwave, etc.
/// </summary>
[ByRefEvent]
public record struct CookedFoodEvent(EntityUid User, EntProtoId Result, int Count = 1);
