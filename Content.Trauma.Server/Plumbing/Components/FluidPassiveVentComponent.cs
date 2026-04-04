namespace Content.Trauma.Server.Plumbing.Components;

[RegisterComponent]
public sealed partial class FluidPassiveVentComponent : Component
{
    [DataField("inlet")]
    public string InletName = "fluid";

    [DataField]
    public float TransferRate = 10f; // units per second
}
