using Content.Client.UserInterface;
using Content.Trauma.Client.UserActions;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.UserInterface;

public sealed class UserActionPanelManager : IUserActionPanelManager
{
    public void InjectPanel(Control container)
    {
        var panel = new UserActionsPanel();
        container.AddChild(panel);
    }
}
