using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared.Input;
using Content.Shared.Whitelist;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Systems;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;

namespace Content.Client._Trauma;

[UsedImplicitly]
public sealed class MartialArtsUIController : UIController, IOnStateChanged<GameplayState>
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [UISystemDependency] private readonly CommonKnowledgeSystem _knowledge = default!;

    public MenuButton? MartialArtsButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.MartialArtsButton;
    private SimpleRadialMenu? _menu;

    public void OnStateEntered(GameplayState state)
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenMartialArtsMenu,
                InputCmdHandler.FromDelegate(_ => ToggleMartialArtsMenu(false)))
            .Register<MartialArtsUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        CommandBinds.Unregister<MartialArtsUIController>();
    }

    private void ToggleMartialArtsMenu(bool centered)
    {
        if (_menu == null)
        {
            // setup window
            var models = GetButtons();

            _menu = new SimpleRadialMenu();
            _menu.SetButtons(models);

            _menu.Open();

            _menu.OnClose += OnWindowClosed;
            _menu.OnOpen += OnWindowOpen;

            if (MartialArtsButton != null)
                MartialArtsButton.SetClickPressed(true);

            if (centered)
            {
                _menu.OpenCentered();
            }
            else
            {
                _menu.OpenOverMouseScreenPosition();
            }
        }
        else
        {
            _menu.OnClose -= OnWindowClosed;
            _menu.OnOpen -= OnWindowOpen;

            if (MartialArtsButton != null)
                MartialArtsButton.SetClickPressed(false);

            CloseMenu();
        }
    }

    public void UnloadButton()
    {
        if (MartialArtsButton == null)
            return;

        MartialArtsButton.OnPressed -= ActionButtonPressed;
    }

    public void LoadButton()
    {
        if (MartialArtsButton == null)
            return;

        MartialArtsButton.OnPressed += ActionButtonPressed;
    }

    private void ActionButtonPressed(BaseButton.ButtonEventArgs args)
    {
        ToggleMartialArtsMenu(true);
    }

    private void OnWindowClosed()
    {
        if (MartialArtsButton != null)
            MartialArtsButton.Pressed = false;

        CloseMenu();
    }

    private void OnWindowOpen()
    {
        if (MartialArtsButton != null)
            MartialArtsButton.Pressed = true;
    }

    private void CloseMenu()
    {
        if (_menu == null)
            return;

        _menu.Dispose();
        _menu = null;
    }

    private IEnumerable<RadialMenuActionOption<EntityUid?>> GetButtons()
    {
        var whitelistSystem = EntitySystemManager.GetEntitySystem<EntityWhitelistSystem>();
        var player = _player.LocalSession?.AttachedEntity;

        var martialArts = new List<RadialMenuActionOption<EntityUid?>>
        {
            new RadialMenuActionOption<EntityUid?>(HandleRadialButtonClick, null)
            {
                //IconSpecifier = RadialMenuIconSpecifier.With(emote.Icon),
                ToolTip = Loc.GetString("no-martial-art")
            }
        };

        var commonKnowledge = _knowledge;
        if (commonKnowledge == null)
            commonKnowledge = EntityManager.System<CommonKnowledgeSystem>();

        if (!(player is { } playerNotNull && commonKnowledge.TryGetKnowledgeEntity(playerNotNull) is { } knowledgeEntity))
            return martialArts;

        var martialArtsList = commonKnowledge.GetMartialArtsForClientDoohickey(knowledgeEntity);

        if (martialArtsList == null)
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
        if (_player.LocalSession?.AttachedEntity is not { } player)
            return;

        var netEnt = EntityManager.GetNetEntity(martialArt);
        EntityManager.RaisePredictiveEvent(new KnowledgeUpdateMartialArts(netEnt));
    }
}
