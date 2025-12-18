using Content.Shared.Inventory;

namespace Content.Goobstation.Shared._Trauma.Chaplain;

/// <summary>
/// A flammable entity has been ignited.
/// </summary>
/// <remarks>
/// This can occur on both <c>Flammable</c> entities as well as <see cref="SmokableComponent"/>.
/// </remarks>
[ByRefEvent]
public struct HolyIgniteEvent : IInventoryRelayEvent
{
    /// <summary>
    /// Amount of firestacks changed. Should be a positive number.
    /// </summary>
    public float FireStacksAdjustment;

    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.WITHOUT_POCKET;
}
