// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;

namespace Content.Trauma.Client.LinkAccount;

[GenerateTypedNameReferences]
public sealed partial class PatronPerksWindow : FancyWindow
{
    public PatronPerksWindow()
    {
        RobustXamlLoader.Load(this);
    }
}
