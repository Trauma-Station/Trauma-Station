// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Circuits;

namespace Content.Trauma.Client.Circuits.UI;

[GenerateTypedNameReferences]
public sealed partial class IntPickerWindow : ConstPickerWindow
{
    public override object Value => new Integer(int.Parse(ValueEdit.Text));

    public IntPickerWindow()
    {
        RobustXamlLoader.Load(this);

        ValueEdit.OnTextChanged += args =>
        {
            CreateButton.Disabled = !int.TryParse(args.Text, out _);
        };
        CreateButton.OnPressed += _ => Create();
    }
}
