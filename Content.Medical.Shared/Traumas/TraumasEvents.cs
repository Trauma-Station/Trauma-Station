// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Traumas;
using Content.Shared.Body;
using Content.Shared.FixedPoint;

namespace Content.Medical.Shared.Traumas;

[ByRefEvent]
public record struct OrganIntegrityChangedEvent(FixedPoint2 OldIntegrity, FixedPoint2 NewIntegrity);

[ByRefEvent]
public record struct OrganDamageSeverityChanged(OrganSeverity OldSeverity, OrganSeverity NewSeverity);

[ByRefEvent]
public record struct OrganDamageSeverityChangedOnWoundable(Entity<InternalChildOrganComponent> Organ, OrganSeverity OldSeverity, OrganSeverity NewSeverity);

/// <summary>
/// Raised on the trauma inflicting wound when a trauma is remvoed.
/// </summary>
[ByRefEvent]
public record struct TraumaBeingRemovedEvent(Entity<TraumaComponent> Trauma);

[ByRefEvent]
public record struct BoneIntegrityChangedEvent(FixedPoint2 OldIntegrity, FixedPoint2 NewIntegrity);

[ByRefEvent]
public record struct BoneSeverityChangedEvent(BoneSeverity OldSeverity, BoneSeverity NewSeverity);
