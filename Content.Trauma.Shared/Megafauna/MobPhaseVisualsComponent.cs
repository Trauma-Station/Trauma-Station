namespace Content.Trauma.Shared.Megafauna;

/// <summary>
/// Component that changes the visuals of the megafauna based on the current phase
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MobPhaseVisualsComponent : Component
{
    /// <summary>
    /// A list of phases. Use generic visualizer to define the states.
    /// </summary>
    [DataField(required: true)]
    public List<int> Phases = new();
}
