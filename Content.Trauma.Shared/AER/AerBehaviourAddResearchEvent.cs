namespace Content.Trauma.Shared.AER;



//i dunno event definition?
[ByRefEvent]
public record struct AerBehaviourAddResearchEvent
{
    public EntityUid Aer;

    //constructor
    public AerBehaviourAddResearchEvent(EntityUid aer)
    {
        Aer = aer;
    }
}