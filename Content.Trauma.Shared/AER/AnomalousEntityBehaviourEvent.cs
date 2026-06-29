namespace Content.Trauma.Shared.AER;



//i dunno event definition?
[ByRefEvent]
public record struct AerBehaviourEvent
{
    public EntityUid Aer;

    //constructor
    public AerBehaviourEvent(EntityUid aer)
    {
        Aer = aer;
    }
}