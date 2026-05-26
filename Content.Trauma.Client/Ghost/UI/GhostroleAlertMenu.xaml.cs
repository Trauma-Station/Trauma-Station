// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;

namespace Content.Trauma.Client.Ghost.UI;

[GenerateTypedNameReferences]
public sealed partial class GhostroleAlertMenu : FancyWindow
{
    public GhostroleAlertMenu()
    {
        RobustXamlLoader.Load(this);
        ConfirmButton.OnPressed += _ => Close();
    }
}
