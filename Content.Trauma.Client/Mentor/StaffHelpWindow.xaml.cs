// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.UserInterface.CustomControls;

namespace Content.Trauma.Client.Mentor;

[GenerateTypedNameReferences]
public sealed partial class StaffHelpWindow : DefaultWindow
{
    public StaffHelpWindow()
    {
        RobustXamlLoader.Load(this);
    }
}
