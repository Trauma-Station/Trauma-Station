namespace Content.Trauma.Shared.Card;

[RegisterComponent, NetworkedComponent]
public sealed partial class AntagCardComponent : Component
{
    /// <summary>
    /// Threat level for sec units to beat you up for.
    /// </summary>
    [DataField]
    public int Threat = 1;
}
