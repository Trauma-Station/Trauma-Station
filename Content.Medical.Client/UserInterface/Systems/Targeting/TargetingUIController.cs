using Content.Client.Gameplay;
using Content.Client.UserInterface.Screens;
using Content.Medical.Client.Targeting;
using Content.Medical.Client.UserInterface.Systems.Targeting.Widgets;
using Content.Medical.Common.Targeting;
using Content.Medical.Shared.Targeting;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Player;

namespace Content.Medical.Client.UserInterface.Systems.Targeting;

public sealed class TargetingUIController : UIController, IOnStateEntered<GameplayState>, IOnSystemChanged<TargetingSystem>
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IEntityNetworkManager _entNet = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private TargetingComponent? _targetingComponent;
    private TargetingControl? TargetingControl;

    public override void Initialize()
    {
        base.Initialize();

        UIManager.OnScreenChanged += screens =>
        {
            if (screens.New is not {} screen)
                return;

            try
            {
                // separated hud has its own container for it
                var container = screen.FindControl<LayoutContainer>("ViewportContainer");
                CreateTargetingControl(container);
            }
            catch (ArgumentException)
            {
                // default hud, just add it directly
                CreateTargetingControl(screen);
            }
        };
    }

    public void OnSystemLoaded(TargetingSystem system)
    {
        system.TargetingStartup += AddTargetingControl;
        system.TargetingShutdown += RemoveTargetingControl;
        system.TargetChange += CycleTarget;
    }

    public void OnSystemUnloaded(TargetingSystem system)
    {
        system.TargetingStartup -= AddTargetingControl;
        system.TargetingShutdown -= RemoveTargetingControl;
        system.TargetChange -= CycleTarget;
    }

    private void CreateTargetingControl(LayoutContainer parent)
    {
        TargetingControl?.Orphan();
        var control = new TargetingControl();
        control.OnSetTarget += CycleTarget;
        parent.AddChild(control);
        TargetingControl = control;
        LayoutContainer.SetAnchorAndMarginPreset(control, LayoutContainer.LayoutPreset.BottomRight, margin: 5);
        UpdateVisibility();
    }

    public void OnStateEntered(GameplayState state)
    {
        UpdateVisibility();
    }

    public void AddTargetingControl(TargetingComponent component)
    {
        _targetingComponent = component;
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (TargetingControl is not {} control)
            return;

        control.SetTargetDollVisible(_targetingComponent != null);

        if (_targetingComponent is {} comp)
            control.SetBodyPartsVisible(comp.Target);
    }

    public void RemoveTargetingControl()
    {
        _targetingComponent = null;
        UpdateVisibility();
    }

    public void CycleTarget(TargetBodyPart bodyPart)
    {
        if (_player.LocalEntity is not {} user
            || !_entMan.TryGetComponent<TargetingComponent>(user, out var comp)
            || comp.Target == bodyPart
            || TargetingControl is not {} control)
            return;

        _entNet.SendSystemNetworkMessage(new ChangeTargetMessage(bodyPart));
        control.SetBodyPartsVisible(bodyPart);
    }
}
