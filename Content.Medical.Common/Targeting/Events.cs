// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Medical.Common.Targeting;

/// <summary>
/// Raised on a mob to try get the bodypart of a target entity it's targeting.
/// </summary>
[ByRefEvent]
public record struct GetTargetedPartEvent(EntityUid Target, EntityUid? Part = null);
