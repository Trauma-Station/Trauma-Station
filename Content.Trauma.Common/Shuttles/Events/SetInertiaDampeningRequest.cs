// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Shuttles;

namespace Content.Trauma.Common.Shuttles.Events;

/// <summary>
/// Raised on the client when it wishes to change the inertial dampening of a ship.
/// </summary>
[Serializable, NetSerializable]
public sealed class SetInertiaDampeningRequest : BoundUserInterfaceMessage
{
    public NetEntity? ShuttleEntityUid { get; set; }
    public InertiaDampeningMode Mode { get; set; }
}
