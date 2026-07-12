namespace Content.Trauma.Shared.AER;



/// <summary>
/// event raised for updating the active status of a AER
/// </summary>
[ByRefEvent]
public record struct AerUpdateActiveStatusEvent
{
    public EntityUid Aer;
    public bool Active;

    //constructor
    public AerUpdateActiveStatusEvent(EntityUid aer, bool active)
    {
        Aer = aer;
        Active = active;
    }
}