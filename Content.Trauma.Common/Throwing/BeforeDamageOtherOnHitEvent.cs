// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;

namespace Content.Trauma.Common.Throwing;

/// <summary>
/// Raised on thrown object before it deals damage to target
/// </summary>
[ByRefEvent]
public record struct BeforeDamageOtherOnHitEvent(EntityUid? User,
    EntityUid Target,
    DamageSpecifier BaseDamage,
    DamageSpecifier BonusDamage,
    bool Cancelled = false);
