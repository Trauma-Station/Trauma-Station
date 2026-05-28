// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Parry;

/// <summary>
/// Raised on an entity when it's attacked in melee. If <see cref="Parried"/> is set to true, the attack will have no effect on the target.
/// </summary>

[ByRefEvent]
public record struct ParryAttemptEvent(EntityUid User, EntityUid Weapon, EntityUid Target, bool Parried = false);
