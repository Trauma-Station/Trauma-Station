// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Common.Religion;

[ByRefEvent]
public record struct BeforeCastTouchSpellEvent(EntityUid Target, bool DoEffects = true, bool Cancelled = false);

[ByRefEvent]
public record struct UserShouldTakeHolyEvent(EntityUid Target, bool WeakToHoly = false, bool ShouldTakeHoly = false);
