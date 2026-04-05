using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Plumbing.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class FluidPumpComponent : Component
{
    [DataField] public bool Enabled = true;
    [DataField] public string InletName = "inlet";
    [DataField] public string OutletName = "outlet";

    /// <summary>
    /// How much fluid to pump
    /// </summary>
    [DataField] public float PumpRate = 10f;

    // The maximum pressure the pump can push against before it stalls
    [DataField] public float MaxOutputPressure = 200.0f;
}
