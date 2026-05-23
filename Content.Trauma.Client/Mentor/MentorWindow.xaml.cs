// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.UserInterface.CustomControls;

namespace Content.Trauma.Client.Mentor;

[GenerateTypedNameReferences]
public sealed partial class MentorWindow : DefaultWindow
{
    public NetUserId SelectedPlayer { get; set; }
    public readonly Dictionary<NetUserId, Button> PlayerDict = new();

    public MentorWindow()
    {
        RobustXamlLoader.Load(this);
    }
}
