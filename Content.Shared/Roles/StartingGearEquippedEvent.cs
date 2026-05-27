namespace Content.Shared.Roles;

/// <summary>
/// Raised directed on an entity when a new starting gear prototype has been equipped.
/// </summary>
[ByRefEvent]
public record struct StartingGearEquippedEvent(EntityUid Entity, IEquipmentLoadout? StartingGear = null) // <Trauma> - Changed to include starting gear prototype for IPC encryption key spawning.
{
    public readonly EntityUid Entity = Entity;
}
