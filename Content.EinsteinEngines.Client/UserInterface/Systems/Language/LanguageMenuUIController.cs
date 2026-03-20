// SPDX-FileCopyrightText: 2025 CerberusWolfie <wb.johnb.willis@gmail.com>
// SPDX-FileCopyrightText: 2025 FoxxoTrystan <45297731+FoxxoTrystan@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.Player;
using System.Numerics;
using Content.Client.Gameplay;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.EinsteinEngines.Client.Language;
using Content.Shared.Input;
using Content.Trauma.Common.Input;
using JetBrains.Annotations;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Utility;

namespace Content.EinsteinEngines.Client.UserInterface.Systems.Language;

public sealed class LanguageMenuUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IResourceCache _cache = default!;

    public const string ButtonName = "LanguageButton";

    private LanguageMenuWindow? _menu;
    private MenuButton? _button;

    public override void Initialize()
    {
        base.Initialize();

        GameTopMenuBarUIController.OnLoad += OnLoadGameBar;
    }
    private void OnLoadGameBar(GameTopMenuBar bar)
    {
        EnsureButton(bar);
    }

    private MenuButton? EnsureButton(GameTopMenuBar bar)
    {
        // first try find it
        foreach (var child in bar.Children)
        {
            if (child.Name == ButtonName)
                return (MenuButton) child;
        }

        // insert at the same index as admin button (so before it)
        var index = bar.AdminButton.GetPositionInParent();

        // add a new button for the first time it's loaded
        var button = new MenuButton()
        {
            Name = ButtonName,
            Icon = _cache.GetResource<TextureResource>(new ResPath("/Textures/Interface/emotes.svg.192dpi.png")).Texture,
            ToolTip = Loc.GetString("game-hud-open-martial-arts-menu-button-tooltip"),
            BoundKey = EinsteinEnginesKeyFunctions.OpenLanguageMenu,
            MinSize = new Vector2(42, 64),
            HorizontalExpand = true,
        };
        button.AddStyleClass(StyleClass.ButtonSquare);
        button.Pressed = _menu != null;
        button.OnPressed += _ => ToggleMartialArtsMenu(false); // not centered on mouse since it's at the top of your screen rn

        bar.AddChild(button);
        button.SetPositionInParent(index);

        return _button = button;


        CommandBinds.Builder.Bind(ContentKeyFunctions.OpenLanguageMenu,
            InputCmdHandler.FromDelegate(_ => ToggleWindow())).Register<LanguageMenuUIController>();
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

        CommandBinds.Builder.Bind(ContentKeyFunctions.OpenLanguageMenu,
            InputCmdHandler.FromDelegate(_ => ToggleWindow())).Register<LanguageMenuUIController>();
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

    public void UnloadButton()
    {
        if (_button is not { })
            return;

        _button.OnPressed -= LanguageButtonPressed;
    }

    public void LoadButton()
    {
        if (_button is not { })
            return;

        _button.OnPressed += LanguageButtonPressed;
    }

    private void LanguageButtonPressed(ButtonEventArgs args)
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
