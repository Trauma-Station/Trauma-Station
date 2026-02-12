// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Content.Medical.Common.Traumas;
using Content.Shared.FixedPoint;

namespace Content.Medical.Shared.Traumas;

// TODO kill
[ByRefEvent]
public record struct OrganIntegrityChangedEvent(FixedPoint2 OldIntegrity, FixedPoint2 NewIntegrity);

[ByRefEvent]
public record struct OrganDamageSeverityChanged(OrganSeverity OldSeverity, OrganSeverity NewSeverity);

[ByRefEvent]
public record struct OrganIntegrityChangedEventOnWoundable(Entity<InternalOrganComponent> Organ, FixedPoint2 OldIntegrity, FixedPoint2 NewIntegrity);

[ByRefEvent]
public record struct OrganDamageSeverityChangedOnWoundable(Entity<InternalOrganComponent> Organ, OrganSeverity OldSeverity, OrganSeverity NewSeverity);
[ByRefEvent]
public record struct TraumaChanceDeductionEvent(FixedPoint2 TraumaSeverity, TraumaType TraumaType, FixedPoint2 ChanceDeduction);

[ByRefEvent]
public record struct BeforeTraumaInducedEvent(FixedPoint2 TraumaSeverity, EntityUid TraumaTarget, TraumaType TraumaType, bool Cancelled = false);

[ByRefEvent]
public record struct TraumaInducedEvent(Entity<TraumaComponent> Trauma, EntityUid TraumaTarget, FixedPoint2 TraumaSeverity, TraumaType TraumaType);

[ByRefEvent]
public record struct TraumaBeingRemovedEvent(Entity<TraumaComponent> Trauma, EntityUid TraumaTarget, FixedPoint2 TraumaSeverity, TraumaType TraumaType);

[ByRefEvent]
public record struct BoneIntegrityChangedEvent(Entity<BoneComponent> Bone, FixedPoint2 OldIntegrity, FixedPoint2 NewIntegrity);

[ByRefEvent]
public record struct BoneSeverityChangedEvent(Entity<BoneComponent> Bone, BoneSeverity OldSeverity, BoneSeverity NewSeverity);
