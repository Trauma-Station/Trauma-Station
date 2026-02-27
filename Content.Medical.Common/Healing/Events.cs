// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.FixedPoint;

namespace Content.Medical.Common.Healing;

/// <summary>
/// Raised on a bodypart to check if it is bleeding.
/// </summary>
[ByRefEvent]
public record struct CheckPartBleedingEvent(bool Bleeding = false);

/// <summary>
/// Raised on a bodypart to try heal its bleeding wounds.
/// </summary>
[ByRefEvent]
public record struct HealBleedingWoundsEvent(float BloodlossModifier, FixedPoint2 BleedStopAbility);

/// <summary>
/// Raised on a bodypart to let any traumas prevent healing with topicals.
/// </summary>
[ByRefEvent]
public record struct PartHealAttemptEvent(bool Cancelled = false, bool Bleeding = false);

/// <summary>
/// Raised on a mob to modify self healing speed with topicals.
/// </summary>
[ByRefEvent]
public record struct ModifySelfHealSpeedEvent(float Modifier = 1f);
