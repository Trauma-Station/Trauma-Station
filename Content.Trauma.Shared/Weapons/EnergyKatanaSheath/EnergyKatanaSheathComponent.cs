namespace Content.Trauma.Shared.Weapons.EnergyKatanaSheath;

[RegisterComponent, NetworkedComponent]
public sealed partial class EnergyKatanaSheathComponent : Component
{
    [DataField]
    public string Slot = "item";
}
