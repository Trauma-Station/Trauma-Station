// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;

namespace Content.Trauma.Shared.Waypointer.Events;

/// <summary>
/// This is a simple action for when someone wants to manage their waypointers.
/// </summary>
public sealed partial class ActionManageWaypointersEvent : InstantActionEvent;

/// <summary>
/// This message is sent from the client when the player wants to toggle all waypointers.
/// </summary>
/// <param name="isActive">Whether the waypointer system is now active or inactive.</param>
[Serializable, NetSerializable]
public sealed class WaypointersToggledMessage(bool isActive) : BoundUserInterfaceMessage
{
    public bool IsActive = isActive;
}

/// <summary>
/// This message is sent from the client when the player wants to toggle a specific waypointer.
/// </summary>
/// <param name="waypointer">The waypointer to be toggled.</param>
[Serializable, NetSerializable]
public sealed class WaypointerStatusChangedMessage(ProtoId<WaypointerPrototype> waypointer) : BoundUserInterfaceMessage
{
    public ProtoId<WaypointerPrototype> ToggledWaypointerProtoId = waypointer;
}
