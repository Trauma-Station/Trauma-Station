// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DeviceLinking;
using Content.Trauma.Shared.Circuits;

namespace Content.Trauma.Client.Circuits.UI;

[GenerateTypedNameReferences]
public sealed partial class BoolPickerWindow : ConstPickerWindow
{
    public override object Value => ToggleButton.Pressed ? SignalState.High : SignalState.Low;

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
