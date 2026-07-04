namespace Content.Trauma.Shared.AER;



//i dunno event definition?
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