namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised on an AmmoProvider to request deets.
/// </summary>
[ByRefEvent]
public struct GetAmmoCountEvent
{
    // <Trauma>
    // This is currently only used for BatteryAmmoProvider
    public float FireCostMultiplier = 1f;

    public GetAmmoCountEvent() { }
    // </Trauma>
    public int Count;
    public int Capacity;
}
