namespace Content.Shared.Weapons.Ranged.Components;

public sealed partial class BatteryAmmoProviderComponent
{
    [ViewVariables, AutoNetworkedField]
    public float ShotsFloat;

    [ViewVariables, AutoNetworkedField]
    public float CapacityFloat;
}
