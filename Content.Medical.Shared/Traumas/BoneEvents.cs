// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.FixedPoint;

namespace Content.Medical.Shared.Traumas;

[ByRefEvent]
public record struct PartBoneDamageChangedEvent(Entity<BoneComponent> Bone, EntityUid Body, FixedPoint2 NewIntegrity);
