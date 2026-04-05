using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Plumbing;

[RegisterComponent, NetworkedComponent]
public sealed partial class FluidSprinklerComponent : Component
{
    [DataField] public bool Enabled = false;

    [DataField] public string InletName = "fluid";

    /// <summary>
    /// If the temperature on the tile exceeds this, the sprinkler pops. 343.15K is roughly 70°C (158°F).
    /// </summary>
    [DataField] public float ThermalActivationThreshold = 343.15f;

    [DataField] public float SprayRange = 2.5f;

    [DataField] public float TransferRate = 20f;
}
