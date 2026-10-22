// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Parry;

/// <summary>
/// Raised on an item that we used to sucessfully parry/reflect attack
/// </summary>
[ByRefEvent]
public record struct ParriedWithEvent(EntityUid User, EntityUid? Attacker);
