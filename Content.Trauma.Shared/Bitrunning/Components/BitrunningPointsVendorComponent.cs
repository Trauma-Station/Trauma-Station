using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Bitrunning.Components;

/// <summary>
/// Makes a <see cref="ShopVendorComponent"/> use bitrunning points to buy items.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BitrunningPointsVendorComponent : Component;
