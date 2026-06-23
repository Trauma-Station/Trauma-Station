namespace Content.Trauma.Client.Spy;

[RegisterComponent]
public sealed partial class BeingScannedComponent : Component
{
    [DataField]
    public EntityUid Scanner;

    [DataField]
    public int MultiShaderOrder = 20;

    [DataField]
    public float Ratio;
}
