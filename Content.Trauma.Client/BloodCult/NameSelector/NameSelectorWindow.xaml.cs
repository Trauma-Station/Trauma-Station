// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;

namespace Content.Trauma.Client.BloodCult.NameSelector;

[GenerateTypedNameReferences]
public sealed partial class NameSelectorWindow : FancyWindow
{
    public event Action<string>? OnSelected;

    public NameSelectorWindow()
    {
        RobustXamlLoader.Load(this);

        AcceptButton.OnPressed += _ => OnSelected?.Invoke(NameEdit.Text);
    }
}
