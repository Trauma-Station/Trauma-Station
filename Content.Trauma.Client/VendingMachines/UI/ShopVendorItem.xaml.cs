// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Client.VendingMachines.UI;

[GenerateTypedNameReferences]
public sealed partial class ShopVendorItem : BoxContainer
{
    public ShopVendorItem(EntProtoId entProto, string text, uint cost)
    {
        RobustXamlLoader.Load(this);

        ItemPrototype.SetPrototype(entProto);

        NameLabel.Text = text;

        CostLabel.Text = cost.ToString();
    }
}
