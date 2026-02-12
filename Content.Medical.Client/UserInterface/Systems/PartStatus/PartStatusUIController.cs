// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.Alerts.Widgets;
using Content.Medical.Client.Targeting;
using Content.Medical.Client.UserInterface.Systems.PartStatus.Widgets;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.PartStatus;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Utility;
using Robust.Shared.Timing;

namespace Content.Medical.Client.UserInterface.Systems.PartStatus;

public sealed class PartStatusUIController : UIController, IOnStateEntered<GameplayState>, IOnSystemChanged<TargetingSystem>
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IEntityNetworkManager _entNet = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    private SpriteSystem _sprite = default!;

    private BodyStatusComponent? _comp;
    private PartStatusControl? PartStatusControl;

    public override void Initialize()
    {
        base.Initialize();

        UIManager.OnScreenChanged += screens =>
        {
            try
            {
                if (screens.New?.FindControl<AlertsUI>("Alerts") is {} alerts)
                    AddPartStatusToAlerts(alerts);
            }
            catch (ArgumentException)
            {
                // don't care if it's a non-game screen or whatever
                // dogshit api has no nullable method :)
            }
        };
    }

    public void OnSystemLoaded(TargetingSystem system)
    {
        system.PartStatusStartup += UpdatePartStatusControl;
        system.PartStatusShutdown += RemovePartStatusControl;
        system.PartStatusUpdate += UpdatePartStatusControl;
    }

    public void OnSystemUnloaded(TargetingSystem system)
    {
        system.PartStatusStartup -= UpdatePartStatusControl;
        system.PartStatusShutdown -= RemovePartStatusControl;
        system.PartStatusUpdate -= UpdatePartStatusControl;
    }

    private void AddPartStatusToAlerts(AlertsUI alerts)
    {
        PartStatusControl?.Orphan();
        var control = new PartStatusControl(this);
        control.OnPartStatusClicked += GetPartStatusMessage;
        PartStatusControl = control;
        alerts.PartStatus.AddChild(control);
        UpdateVisibility();
    }

    public void OnStateEntered(GameplayState state)
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (PartStatusControl is not {} control)
            return;

        control.SetVisible(_comp != null);
        if (_comp is {} comp)
            control.SetTextures(comp.BodyStatus);
    }

    public void RemovePartStatusControl()
    {
        _comp = null;
        UpdateVisibility();
    }

    public void UpdatePartStatusControl(BodyStatusComponent comp)
    {
        _comp = comp;
        UpdateVisibility();
    }

    public Texture GetTexture(SpriteSpecifier specifier)
    {
        _sprite ??= _entMan.System<SpriteSystem>();

        return _sprite.Frame0(specifier);
    }

    public void GetPartStatusMessage()
    {
        if (_player.LocalEntity is not {} user
            || !_entMan.HasComponent<BodyStatusComponent>(user)
            || !_timing.IsFirstTimePredicted)
            return;

        _entNet.SendSystemNetworkMessage(new GetPartStatusEvent());
    }
}
