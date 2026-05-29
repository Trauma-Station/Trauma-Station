// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Circuits;

namespace Content.Trauma.Client.Circuits.UI;

[GenerateTypedNameReferences]
public sealed partial class BoolPickerWindow : ConstPickerWindow
{
    public override object Value => ToggleButton.Pressed ? True.Instance : False.Instance;

    public BoolPickerWindow()
    {
        RobustXamlLoader.Load(this);

        ToggleButton.OnPressed += _ =>
        {
            ToggleButton.Text = ToggleButton.Pressed.ToString();
        };
        CreateButton.OnPressed += _ => Create();
    }
}
