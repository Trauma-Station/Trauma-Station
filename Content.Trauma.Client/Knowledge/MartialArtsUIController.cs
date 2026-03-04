// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar;
using Content.Shared.Whitelist;
using Content.Trauma.Common.Input;
using Content.Trauma.Common.Knowledge;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;

namespace Content.Trauma.Client.Knowledge;

[UsedImplicitly]
public sealed class MartialArtsUIController : UIController, IOnStateChanged<GameplayState>
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [UISystemDependency] private KnowledgeSystem _knowledge = default!;

    private SimpleRadialMenu? _menu;

    public override void Initialize()
    {
        base.Initialize();

        var menuBar = UIManager.GetUIController<GameTopMenuBarUIController>();
        menuBar.OnMartialArtsPressed += () => ToggleMartialArtsMenu(true);
    }

    public void OnStateEntered(GameplayState state)
    {
        var menuBar = UIManager.GetUIController<GameTopMenuBarUIController>();
        menuBar.OnMartialArtsPressed += OpenMenuFromAction;

        CommandBinds.Builder
            .Bind(TraumaKeyFunctions.OpenMartialArtsMenu,
                InputCmdHandler.FromDelegate(_ => ToggleMartialArtsMenu(false)))
            .Register<MartialArtsUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        var menuBar = UIManager.GetUIController<GameTopMenuBarUIController>();
        menuBar.OnMartialArtsPressed -= OpenMenuFromAction;
        CommandBinds.Unregister<MartialArtsUIController>();
        CloseMenu();
    }

    private void OpenMenuFromAction() => ToggleMartialArtsMenu(true);

    private void ToggleMartialArtsMenu(bool centered)
    {
        if (_menu is { })
        {
            CloseMenu();
            return;
        }
        // setup window
        var models = GetButtons().ToList();

        _menu = new SimpleRadialMenu();
        _menu.SetButtons(models);

        _menu.Open();

        _menu.OnClose += OnWindowClosed;

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
    }

    private IEnumerable<RadialMenuActionOption<EntityUid?>> GetButtons()
    {
        var whitelistSystem = EntitySystemManager.GetEntitySystem<EntityWhitelistSystem>();

        var martialArts = new List<RadialMenuActionOption<EntityUid?>>
        {
            new RadialMenuActionOption<EntityUid?>(HandleRadialButtonClick, null)
            {
                //IconSpecifier = RadialMenuIconSpecifier.With(emote.Icon),
                ToolTip = Loc.GetString("no-martial-art")
            }
        };

        if (_player.LocalEntity is not { } player || _knowledge.GetMartialArtsForClientDoohickey(player) is not { } martialArtsList)
            return martialArts;

        foreach (var martialArt in martialArtsList)
        {
            var actionOption = new RadialMenuActionOption<EntityUid?>(HandleRadialButtonClick, martialArt.Item1)
            {
                //IconSpecifier = RadialMenuIconSpecifier.With(emote.Icon),
                ToolTip = Loc.GetString(martialArt.Item2)
            };
            martialArts.Add(actionOption);
        }

        return martialArts;
    }

    private void HandleRadialButtonClick(EntityUid? martialArt)
    {
        if (_player.LocalEntity is not { })
            return;

        var netEnt = EntityManager.GetNetEntity(martialArt);
        EntityManager.RaisePredictiveEvent(new KnowledgeUpdateMartialArtsEvent(netEnt));
    }
}
