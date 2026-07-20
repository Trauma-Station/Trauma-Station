// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.VendingMachines;

/// <summary>
/// Makes a <see cref="ShopVendorComponent"/> use mining points to buy items.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PointsVendorComponent : Component;
