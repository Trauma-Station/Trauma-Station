// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Medical.Shared.Surgery;

/// <summary>
///     Raised on the target entity.
/// </summary>
[ByRefEvent]
public record struct SurgeryStepDamageChangeEvent(EntityUid User, EntityUid Body, EntityUid Part, EntityUid Step);
