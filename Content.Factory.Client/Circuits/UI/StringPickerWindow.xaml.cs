// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Factory.Shared.Circuits;

namespace Content.Factory.Client.Circuits.UI;

[GenerateTypedNameReferences]
public sealed partial class StringPickerWindow : ConstPickerWindow
{
    public override object Value => ValueEdit.Text;

    public StringPickerWindow()
    {
        RobustXamlLoader.Load(this);

        CreateButton.OnPressed += _ => Create();
    }
}
