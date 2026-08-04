namespace Content.Trauma.Client.Spy;

[RegisterComponent]
public sealed partial class BeingScannedComponent : Component
{
    [DataField]
    public EntityUid Scanner;

    [DataField]
    public float Ratio;
}
