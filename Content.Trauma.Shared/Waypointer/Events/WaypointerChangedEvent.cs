// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory;

namespace Content.Trauma.Shared.Waypointer.Events;

/// <summary>
/// Whenever a clothing that shows waypointers is equipped.
/// </summary>
[ByRefEvent]
public record struct WaypointerChangedEvent() : IInventoryRelayEvent
{
    public HashSet<ProtoId<WaypointerPrototype>> Waypointers = [];
    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.WITHOUT_POCKET;
}
