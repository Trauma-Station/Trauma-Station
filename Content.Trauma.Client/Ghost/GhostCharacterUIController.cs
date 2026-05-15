// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Systems.Ghost;
using Content.Client.UserInterface.Systems.Ghost.Widgets;
using Content.Trauma.Client.Ghost.UI;
using Content.Trauma.Shared.Ghost;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;

namespace Content.Trauma.Client.Ghost;

public sealed partial class GhostCharacterUIController : UIController
{
    [UISystemDependency] private readonly GhostCharacterSystem _character = default!;

    public const string ButtonName = "PickCharacterButton";

    private GhostCharacterWindow? _window;

    public override void Initialize()
    {
        base.Initialize();

        GhostUIController.OnGuiLoaded += OnGuiLoaded;
    }

    private void OnGuiLoaded(GhostGui gui)
    {
        var container = gui.GetChild(0);
        // don't add duplicate buttons
        foreach (var child in container.Children)
        {
            if (child.Name == ButtonName)
                return; // already added
        }

        var button = new Button()
        {
            Name = ButtonName,
            Text = Loc.GetString("ghost-gui-pick-character-button")
        };
        button.OnPressed += _ => TogglePickCharacter();
        container.AddChild(button);
    }

    private void TogglePickCharacter()
    {
        if (_window is {})
        {
            _window.Close();
            return;
        }

        _window = new(_character.GetLocalData() ?? new());
        _window.OpenCentered();
        _window.OnSlotSet += slot => _character.SetGhostRoleSlot(slot);
        _window.OnClose += () =>
        {
            _window = null;
        };
    }
}
