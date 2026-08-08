// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;

namespace Content.Trauma.Client.Bitrunning.UI.Disk;

[GenerateTypedNameReferences]
public sealed partial class BitrunningDiskWindow : FancyWindow
{
    public event Action<string>? OnSelected;

    public BitrunningDiskWindow()
    {
        RobustXamlLoader.Load(this);
    }

    public void SetState(List<string> options, string? selectedOption)
    {
        OptionsContainer.RemoveAllChildren();

        if (selectedOption != null)
        {
            var selectedLabel = new Label
            {
                Text = Loc.GetString("bitrunning-disk-ui-selected"),
            };
            OptionsContainer.AddChild(selectedLabel);
            return;
        }

        foreach (var option in options)
        {
            var currentOption = option;
            var button = new Button
            {
                Text = option,
            };
            button.OnPressed += _ => OnSelected?.Invoke(currentOption);
            OptionsContainer.AddChild(button);
        }
    }
}
