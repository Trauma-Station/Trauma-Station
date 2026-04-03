// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Gameplay;
using Content.Client.UserInterface.Screens;
using Content.Trauma.Client.UserActions;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Trauma.Client.UserInterface;

public sealed class UserActionsPanelUIController : UIController, IOnStateEntered<GameplayState>
{
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SeparatedChatGameScreen.OnCreated += InjectPanel;
    }

    public void OnStateEntered(GameplayState state)
    {
        if (_uiManager.ActiveScreen is not SeparatedChatGameScreen screen)
            return;

        InjectPanel(screen);
    }

    public void InjectPanel(SeparatedChatGameScreen container)
    {
        if (container.UserActionsPlaceholder.ChildCount > 0)
            return;

        var panel = new UserActionsPanel();
        container.UserActionsPlaceholder.AddChild(panel);
    }
}
