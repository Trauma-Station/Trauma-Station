// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Traitor;

/// <summary>
/// Raised when an uplink is assigned to someone.
/// </summary>
/// <param name="User">The entity that owns the uplink, such as the traitor's physical body.</param>
/// <param name="Uplink">The the entity that represents the uplink itself. Contains the store and the jobboard.</param>
/// <param name="Host">The entity the uplink is originally hosted in at first. Most likely a PDA.</param>
[ByRefEvent]
public record struct UplinkAssignedEvent(EntityUid User, EntityUid Uplink, EntityUid Host);

/// <summary>
/// A pre-existing uplink is linked to a new host.
/// </summary>
/// <param name="Uplink">The the entity that represents the uplink itself. Contains the store and the jobboard</param>
/// <param name="Host">The new host. Could be an implant</param>
[ByRefEvent]
public record struct UplinkLinkedEvent(EntityUid Uplink, EntityUid Host);
