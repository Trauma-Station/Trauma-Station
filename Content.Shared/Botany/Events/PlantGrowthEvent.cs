using Robust.Shared.Serialization;

namespace Content.Shared.Botany.Events;

/// <summary>
/// Event of plant growing ticking.
/// </summary>
[ByRefEvent]
//[Serializable, NetSerializable] // Trauma - this isnt a networked event or used in yml
public readonly record struct PlantGrowEvent(NetEntity Tray);

/// <summary>
/// Event raised when a harvest is attempted.
/// </summary>
[ByRefEvent]
// <Trauma> - made it a struct, still cancellable despite the shitty event name
public record struct DoHarvestEvent(EntityUid User, EntityUid Target, bool Cancelled = false)
{
    public void Cancel()
    {
        Cancelled = true;
    }
}
// </Trauma>

/// <summary>
/// Event raised after a harvest is attempted.
/// </summary>
[ByRefEvent]
// <Trauma> - make it a non-cancellable struct
public record struct AfterDoHarvestEvent(EntityUid User, EntityUid Target);
// </Trauma>
