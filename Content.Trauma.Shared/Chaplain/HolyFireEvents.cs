using Content.Shared.Inventory;

namespace Content.Trauma.Shared.Chaplain;
// NOTE: These components are currently not raised on the client, only on the server.

/// <summary>
/// An entity has had an existing effect applied to it.
/// </summary>
/// <remarks>
/// This does not necessarily mean the effect is strong enough to fully extinguish the entity in one go.
/// </remarks>
[ByRefEvent]
public struct HolyExtinguishEvent : IInventoryRelayEvent
{
    /// <summary>
    /// Amount of firestacks changed. Should be a negative number.
    /// </summary>
    public float FireStacksAdjustment;

    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.WITHOUT_POCKET;
}

/// <summary>
/// A flammable entity has been extinguished.
/// </summary>
/// <remarks>
/// This can occur on both <c>Flammable</c> entities as well as <see cref="SmokableComponent"/>.
/// </remarks>
/// <seealso cref="ExtinguishEvent"/>
[ByRefEvent]
public struct HolyExtinguishedEvent;

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
