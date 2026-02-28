// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.MartialArts;

[ByRefEvent]
public record struct GetPerformedAttackTypesEvent(List<ComboAttackType>? AttackTypes = null);
