// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;

namespace Content.Goobstation.Shared.SlaughterDemon;

/// <summary>
/// Raised on the entity that gets devoured
/// </summary>
[ByRefEvent]
public record struct SlaughterDevourAttemptEvent(EntityUid Devoured, EntityUid Devourer, bool Cancelled = false);

/// <summary>
/// Doafter for when a slaughter demon is trying to devour a mob.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SlaughterDevourDoAfterEvent : SimpleDoAfterEvent;
