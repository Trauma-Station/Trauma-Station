// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.VendingMachines;

[Serializable, NetSerializable]
public sealed class ShopVendorPurchaseMessage(int index) : BoundUserInterfaceMessage
{
    public readonly int Index = index;
}
