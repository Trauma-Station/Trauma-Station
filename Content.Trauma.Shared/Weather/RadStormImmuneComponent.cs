namespace Content.Trauma.Shared.Weather;

/// <summary>
/// Makes an entity not take damage/effects from radiation storms
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RadStormImmuneComponent : Component;
