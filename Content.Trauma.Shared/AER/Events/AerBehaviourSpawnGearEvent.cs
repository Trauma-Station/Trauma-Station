namespace Content.Trauma.Shared.AER;

/// <summary>
/// event raised for spawning an aer I.D. gear on an aer behaviour
/// </summary>
[ByRefEvent]
public record struct AerBehaviourSpawnGearEvent
{
    public EntityUid Aer;

    //constructor
    public AerBehaviourSpawnGearEvent(EntityUid aer)
    {
        Aer = aer;
    }
}