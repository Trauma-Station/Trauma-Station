// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Wounds;
using Content.Shared.FixedPoint;

namespace Content.Medical.Shared.Wounds;

[ByRefEvent]
public record struct WoundAddedEvent(WoundComponent Component, WoundableComponent Woundable);

[ByRefEvent]
public record struct WoundSeverityPointChangedEvent(WoundComponent Component, FixedPoint2 OldSeverity, FixedPoint2 NewSeverity, FixedPoint2? Overflow = null);

[ByRefEvent]
public record struct WoundSeverityChangedEvent(WoundSeverity OldSeverity, WoundSeverity NewSeverity);

[ByRefEvent]
public record struct WoundHealAttemptEvent(Entity<WoundableComponent> Woundable, bool IgnoreBlockers = false, bool Cancelled = false);

[ByRefEvent]
public record struct WoundHealAttemptOnWoundableEvent(EntityUid Wound, bool Cancelled = false);

[Serializable, DataRecord]
public partial record struct WoundableSeverityMultiplier(FixedPoint2 Change, string Identifier = "Unspecified");

[Serializable, DataRecord]
public partial record struct WoundableHealingMultiplier(FixedPoint2 Change, string Identifier = "Unspecified");
