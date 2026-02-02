using Content.Shared.Alert;
using Content.Shared.Inventory;

namespace Content.Trauma.Shared.Chaplain;

/// <summary>
/// This allows a character to resist the holy fire effect.
/// </summary>
public sealed partial class ResistHolyFireAlertEvent : BaseAlertEvent;

// NOTE: These events are currently not raised on the client, only on the server.

/// <summary>
/// A flammable entity has been extinguished.
/// </summary>
/// <remarks>
/// This can occur on both <c>Flammable</c> entities as well as <see cref="SmokableComponent"/>.
/// </remarks>
/// <seealso cref="ExtinguishEvent"/>
[ByRefEvent]
public record struct HolyExtinguishedEvent;

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
