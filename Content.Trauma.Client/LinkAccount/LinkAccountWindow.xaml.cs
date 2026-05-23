// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.UserInterface.CustomControls;

namespace Content.Trauma.Client.LinkAccount;

[GenerateTypedNameReferences]
public sealed partial class LinkAccountWindow : DefaultWindow
{
    public LinkAccountWindow()
    {
        RobustXamlLoader.Load(this);
    }
}
