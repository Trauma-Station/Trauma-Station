// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Trauma.Common.MartialArts;

/// <summary>
///     Raised when a martial arts combo attack is performed. Contains information about
///     the performer, target, weapon used, and the type of combo attack.
/// </summary>
[ByRefEvent]
public record struct ComboAttackPerformedEvent(EntityUid Performer, EntityUid Target, EntityUid Weapon, ComboAttackType Type);

[Serializable, NetSerializable]
public enum ComboAttackType : byte
{
    Harm,
    HarmLight,
    Disarm,
    Grab,
    Hug,
}
