// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Shuttles.Events;

/// <summary>
/// Raised on the client when it wishes to change the inertial dampening of a ship.
/// </summary>
[Serializable, NetSerializable]
public sealed class SetInertiaDampeningRequest : BoundUserInterfaceMessage
{
    public NetEntity? ShuttleEntityUid { get; set; }
    public InertiaDampeningMode Mode { get; set; }
}
