// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Ghost;

/// <summary>
/// Raised on a ghost role entity after a mind is created and assigned to its spawned mob.
/// </summary>
[ByRefEvent]
public record struct GhostRoleCreatedMindEvent(EntityUid Mob, EntityUid Mind);
