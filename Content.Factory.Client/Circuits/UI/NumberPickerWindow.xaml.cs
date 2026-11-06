// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Factory.Shared.Circuits;

namespace Content.Factory.Client.Circuits.UI;

[GenerateTypedNameReferences]
public sealed partial class NumberPickerWindow : ConstPickerWindow
{
    public override object Value => new Number(float.Parse(ValueEdit.Text));

    public NumberPickerWindow()
    {
        RobustXamlLoader.Load(this);

        ValueEdit.OnTextChanged += args =>
        {
            CreateButton.Disabled = !float.TryParse(args.Text, out var value) || Single.IsNaN(value);
        };
        CreateButton.OnPressed += _ => Create();
    }
}
