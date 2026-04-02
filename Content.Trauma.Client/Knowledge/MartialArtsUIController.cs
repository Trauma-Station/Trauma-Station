// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Gameplay;
using Content.Client.Popups;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Trauma.Common.Input;
using Content.Trauma.Common.Knowledge;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.ContentPack;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Trauma.Client.Knowledge;

public sealed class MartialArtsUIController : UIController, IOnStateChanged<GameplayState>
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IResourceCache _cache = default!;
    [UISystemDependency] private readonly KnowledgeSystem _knowledge = default!;
    [UISystemDependency] private readonly PopupSystem _popup = default!;

    public const string ButtonName = "MartialArtsButton";
    public static readonly ResPath NonePath = new("/Textures/Interface/Radial/close_normal.png");

    private SimpleRadialMenu? _menu;
    private MenuButton? _button;

    public override void Initialize()
    {
        base.Initialize();

        GameTopMenuBarUIController.OnLoad += OnLoadGameBar;
    }

    public void OnStateEntered(GameplayState state)
    {
        CommandBinds.Builder
            .Bind(TraumaKeyFunctions.OpenMartialArtsMenu,
                InputCmdHandler.FromDelegate(_ => ToggleMartialArtsMenu(false)))
            .Register<MartialArtsUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        CommandBinds.Unregister<MartialArtsUIController>();
        CloseMenu();
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
            BoundKey = TraumaKeyFunctions.OpenMartialArtsMenu,
            MinSize = new Vector2(42, 64),
            HorizontalExpand = true,
        };
        button.AddStyleClass(StyleClass.ButtonSquare);
        button.Pressed = _menu != null;
        button.OnPressed += _ => ToggleMartialArtsMenu(false); // not centered on mouse since it's at the top of your screen rn

        bar.AddChild(button);
        button.SetPositionInParent(index);

        return _button = button;
    }

    private void OpenMenuFromAction() => ToggleMartialArtsMenu(true);

    private void ToggleMartialArtsMenu(bool centered)
    {
        if (_menu is { })
        {
            CloseMenu();
            return;
        }

        // setup window if there are any martial arts to use
        var buttons = GetButtons();
        if (buttons.Count < 2) // always have 1 from no martial art option
        {
            var player = _player.LocalEntity;
            _popup.PopupClient(Loc.GetString("knowledge-no-martial-art"), player);
            _button?.Pressed = false;
            return;
        }

        _menu = new SimpleRadialMenu();
        _menu.SetButtons(buttons);

        _menu.Open();

        _menu.OnClose += OnWindowClosed;

        _button?.Pressed = true;

        if (centered)
        {
            _menu.OpenCentered();
        }
        else
        {
            _menu.OpenOverMouseScreenPosition();
        }
    }

    private void OnWindowClosed()
    {
        CloseMenu();
    }

    private void CloseMenu()
    {
        if (_menu == null)
            return;

        _menu.OnClose -= OnWindowClosed;

        _menu.Close();
        _menu = null;
        _button?.Pressed = false;
    }

    private List<RadialMenuActionOption<EntProtoId?>> GetButtons()
    {
        var martialArts = new List<RadialMenuActionOption<EntProtoId?>>
        {
            new RadialMenuActionOption<EntProtoId?>(_knowledge.ChangeMartialArt, null)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(new SpriteSpecifier.Texture(NonePath)),
                ToolTip = Loc.GetString("no-martial-art")
            }
        };

        if (_player.LocalEntity is not {} player)
            return martialArts;

        var arts = _knowledge.GetMartialArtsForClientDoohickey(player);
        foreach (var martialArt in arts)
        {
            var actionOption = new RadialMenuActionOption<EntProtoId?>(_knowledge.ChangeMartialArt, martialArt.Item2)
            {
                IconSpecifier = RadialMenuEntityIconSpecifier.With(martialArt.Item1),
                ToolTip = Loc.GetString(martialArt.Item3)
            };
            martialArts.Add(actionOption);
        }

        return martialArts;
    }
}
