// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Screens;
using Content.Trauma.Client.UserActions;
using Robust.Client.UserInterface.Controllers;

namespace Content.Trauma.Client.UserInterface;

public sealed class UserActionsPanelUIController : UIController
{
    public override void Initialize()
    {
        base.Initialize();

        SeparatedChatGameScreen.OnCreated += InjectPanel;
    }

    public void InjectPanel(SeparatedChatGameScreen container)
    {
        var panel = new UserActionsPanel();
        container.UserActionsPlaceholder.AddChild(panel);
    }
}
