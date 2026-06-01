// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;

namespace Content.Trauma.Client.CosmicCult.UI;

[GenerateTypedNameReferences]
public sealed partial class CosmicDeconvertedMenu : FancyWindow
{
    public CosmicDeconvertedMenu()
    {
        RobustXamlLoader.Load(this);

        ConfirmButton.OnPressed += _ => Close();
    }
}
