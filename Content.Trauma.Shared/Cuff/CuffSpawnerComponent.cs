namespace Content.Trauma.Shared.Cuff;

[RegisterComponent, NetworkedComponent]
public sealed partial class CuffSpawnerComponent : Component
{
    [DataField]
    public EntProtoId HandcuffId = "Handcuffs";
}
