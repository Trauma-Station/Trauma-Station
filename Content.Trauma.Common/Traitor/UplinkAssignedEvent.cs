// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Traitor;

/// <summary>
/// Raised when an uplink is assigned to someone.
/// </summary>
/// <param name="Mind">The mind of the person who owns the uplink.</param>
/// <param name="Store">The entity of the uplink store.</param>
/// <param name="Host">The entity the uplink is originally hosted in at first. Most likely a PDA.</param>
[ByRefEvent]
public record struct UplinkAssignedEvent(EntityUid Mind, EntityUid Store, EntityUid Host);
