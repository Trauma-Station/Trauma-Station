// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Trauma.Client.Language;
using Content.Trauma.Common.Input;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Utility;

namespace Content.Trauma.Client.UserInterface.Systems.Language;

[UsedImplicitly]
public sealed class LanguageMenuUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    private LanguageMenuWindow? _menu;
    private MenuButton? _button;

    public override void Initialize()
    {
        base.Initialize();

        GameTopMenuBarUIController.OnLoad += OnLoadGameBar;
        GameTopMenuBarUIController.OnUnload += OnUnloadGameBar;
    }

    private void OnLoadGameBar(GameTopMenuBar bar)
    {
        _button = bar.LanguageButton;

        LoadButton();
    }

    private void OnUnloadGameBar(GameTopMenuBar bar)
    {
        UnloadButton();
        _button = null;
    }

    public void LoadButton()
    {
        if (_button is not { })
            return;

        _button.OnPressed += LanguageButtonPressed;
    }

    public void UnloadButton()
    {
        if (_button is not { } || _button.Disposed)
            return;

        _button.OnPressed -= LanguageButtonPressed;
    }

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_menu is not { });

        _menu = UIManager.CreateWindow<LanguageMenuWindow>();
        LayoutContainer.SetAnchorPreset(_menu, LayoutContainer.LayoutPreset.CenterTop);

        _menu.OnClose += () =>
        {
            if (_button is { })
                _button.Pressed = false;
        };
        _menu.OnOpen += () =>
        {
            if (_button is { })
                _button.Pressed = true;
        };

        CommandBinds.Builder
            .Bind(TraumaKeyFunctions.OpenLanguageMenu,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<LanguageMenuUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        if (_menu is { })
        {
            _menu.Dispose();
            _menu = null;
        }

        CommandBinds.Unregister<LanguageMenuUIController>();
    }

    private void LanguageButtonPressed(BaseButton.ButtonEventArgs args)
    {
        ToggleWindow();
    }

    private void ToggleWindow()
    {
        if (_menu is not { })
            return;

        if (_button is { })
            _button.SetClickPressed(!_menu.IsOpen);

        if (_menu.IsOpen)
            _menu.Close();
        else
            _menu.Open();
    }
}
