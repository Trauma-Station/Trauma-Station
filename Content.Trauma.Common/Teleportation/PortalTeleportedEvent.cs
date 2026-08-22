// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Teleportation;

/// <summary>
/// Event raised on an entity after it goes through a portal and is teleported to either a destination portal or random position if that's null.
/// </summary>
[ByRefEvent]
public record struct PortalTeleportedEvent(EntityUid Source, EntityUid? Dest);
