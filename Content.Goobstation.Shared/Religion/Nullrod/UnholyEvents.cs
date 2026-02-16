// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 SolsticeOfTheWinter <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 TheBorzoiMustConsume <197824988+TheBorzoiMustConsume@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.Religion.Nullrod;

[ByRefEvent]
public record struct DamageUnholyEvent(EntityUid Target, EntityUid? Origin = null, bool ShouldTakeHoly = false);

[ByRefEvent]
public readonly record struct UnholyStatusChangedEvent(EntityUid Target, EntityUid Source, bool Added);

[ByRefEvent]
public record struct UserShouldTakeHolyEvent(EntityUid Target, bool WeakToHoly = false, bool ShouldTakeHoly = false);

[ByRefEvent]
public record struct BibleSmiteAttemptEvent(EntityUid Target, bool ShouldSmite = false);
