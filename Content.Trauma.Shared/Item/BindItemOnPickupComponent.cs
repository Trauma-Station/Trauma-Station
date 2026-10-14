namespace Content.Trauma.Shared.Item;

/// <summary>
/// Raises a <see cref="BindItemEvent"/> on the wearer when this item is picked up.
/// This allows systems on the wearer to keep a reference to the picked up item.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BindItemOnPickupComponent : Component;
