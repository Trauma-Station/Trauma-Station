// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Roles;

/// <summary>
/// Raised on a role after it has been added to a mind, or when a mind is added to a mob.
/// </summary>
[ByRefEvent]
public record struct RoleGotAddedEvent(EntityUid Mind, EntityUid? Mob);

/// <summary>
/// Raised on a role when its mind is being added to a mob.
/// </summary>
[ByRefEvent]
public record struct RoleMindAddedEvent(EntityUid Mind, EntityUid Mob);

/// <summary>
/// Raised on a role when it is being removed from a mind, just before deleting it.
/// </summary>
[ByRefEvent]
public record struct RoleGotRemovedEvent(EntityUid Mind, EntityUid? Mob);

/// <summary>
/// Raised on a role when its mind is being removed from a mob.
/// The role will still exist.
/// </summary>
[ByRefEvent]
public record struct RoleMindRemovedEvent(EntityUid Mind, EntityUid Mob);
