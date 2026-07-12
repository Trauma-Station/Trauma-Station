namespace Content.Trauma.Shared.AER;



/// <summary>
/// event raised for giving research on an aer behaviour
/// </summary>
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