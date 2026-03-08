// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Damage;

/// <summary>
/// Raised on an entity after it has taken immediate stamina damage.
/// Overtime stamina damage from batong etc is not counted.
/// </summary>
[ByRefEvent]
public record struct TookStaminaDamageEvent(EntityUid Target, float Amount);
