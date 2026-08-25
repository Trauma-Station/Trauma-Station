// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Reagent;

namespace Content.Goobstation.Client.Chemistry.UI;

[GenerateTypedNameReferences]
public sealed partial class EnergyReagentCardControl : Control
{
    public readonly ProtoId<ReagentPrototype> Reagent;
    public readonly int EnergyCost;
    public bool IsDisabled => MainButton.Disabled;

    public Action<ProtoId<ReagentPrototype>>? OnPressed;

    public EnergyReagentCardControl(ReagentPrototype proto, int cost)
    {
        RobustXamlLoader.Load(this);

        Reagent = proto.ID;
        EnergyCost = cost;
        ColorPanel.PanelOverride = new StyleBoxFlat { BackgroundColor = proto.SubstanceColor };
        ReagentNameLabel.Text = proto.LocalizedName;

        MainButton.OnPressed += args => OnPressed?.Invoke(Reagent);
    }

    public void SetDisabled(bool disabled, string tooltip)
    {
        if (disabled == IsDisabled)
            return;

        MainButton.Disabled = disabled;
        if (disabled)
        {
            // Gray out the card when disabled
            Modulate = Color.Gray;
            ToolTip = tooltip;
        }
        else
        {
            Modulate = Color.White;
            ToolTip = null;
        }
    }

    public void SetAmount(int amount)
    {
        var total = EnergyCost * amount;
        FillLabel.Text = $"{total} J ({EnergyCost} J/u)";
    }
}
