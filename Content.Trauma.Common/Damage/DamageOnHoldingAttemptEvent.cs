// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Damage;

/// <summary>
/// Raised on the mob holding a DamageOnHolding item and its gloves to prevent being damaged.
/// </summary>
[ByRefEvent]
public record struct DamageOnHoldingAttemptEvent(EntityUid Source, bool Cancelled = false);
