// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;

namespace Content.Trauma.Common.Weapons;

/// <summary>
/// Raised on the user to calculate potential damage bonuses or decreases.
/// </summary>
[ByRefEvent]
public record struct GetUserMeleeDamageEvent(EntityUid Weapon, DamageSpecifier Damage, List<DamageModifierSet> Modifiers);
