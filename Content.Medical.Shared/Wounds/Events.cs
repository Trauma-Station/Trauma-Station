// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Wounds;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Medical.Shared.Wounds;

[ByRefEvent]
public record struct WoundAddedEvent(WoundComponent Component, WoundableComponent Woundable, WoundableComponent RootWoundable);

[ByRefEvent]
public record struct WoundAddedOnBodyEvent(Entity<WoundComponent> Wound, WoundableComponent Woundable, WoundableComponent RootWoundable);

[ByRefEvent]
public record struct WoundRemovedEvent(WoundComponent Component, WoundableComponent OldWoundable, WoundableComponent OldRootWoundable);

[ByRefEvent]
public record struct WoundableAttachedEvent(EntityUid ParentWoundableEntity, WoundableComponent Component);

[ByRefEvent]
public record struct WoundableDetachedEvent(EntityUid ParentWoundableEntity, WoundableComponent Component);

[ByRefEvent]
public record struct WoundSeverityPointChangedEvent(WoundComponent Component, FixedPoint2 OldSeverity, FixedPoint2 NewSeverity, FixedPoint2? Overflow = null);

[ByRefEvent]
public record struct WoundSeverityPointChangedOnBodyEvent(Entity<WoundComponent> Wound, FixedPoint2 OldSeverity, FixedPoint2 NewSeverity);

[ByRefEvent]
public record struct WoundSeverityChangedEvent(WoundSeverity OldSeverity, WoundSeverity NewSeverity);

[ByRefEvent]
public record struct WoundableIntegrityChangedEvent(FixedPoint2 OldIntegrity, FixedPoint2 NewIntegrity);

[ByRefEvent]
public record struct WoundableSeverityChangedEvent(WoundableSeverity OldSeverity, WoundableSeverity NewSeverity);

[ByRefEvent]
public record struct WoundHealAttemptEvent(Entity<WoundableComponent> Woundable, bool IgnoreBlockers = false, bool Cancelled = false);

[ByRefEvent]
public record struct WoundHealAttemptOnWoundableEvent(Entity<WoundComponent> Wound, bool Cancelled = false);

[Serializable, DataRecord]
public partial record struct WoundableSeverityMultiplier(FixedPoint2 Change, string Identifier = "Unspecified");

[Serializable, DataRecord]
public partial record struct WoundableHealingMultiplier(FixedPoint2 Change, string Identifier = "Unspecified");
