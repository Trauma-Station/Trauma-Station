// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;

namespace Content.Medical.Shared.Surgery.Conditions;

/// <summary>
///     Raised on the entity that is receiving surgery.
/// </summary>
[ByRefEvent]
public record struct SurgeryValidEvent(EntityUid Body, EntityUid Part, bool Cancelled = false, BodyPartType PartType = default, BodyPartSymmetry? Symmetry = null);
