// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Medical.Client.Targeting;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Trauma.Common.CCVar;
using Content.Trauma.Common.Input;
using Content.Trauma.Common.MouseWheel;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Input;

namespace Content.Trauma.Client.MouseWheel;

public sealed partial class MouseWheelSystem : CommonMouseWheelSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IInputManager _inputMan = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    [Dependency] private InputSystem _input = default!;
    [Dependency] private TargetingSystem _targeting = default!;

    private bool _zoom;
    private bool _rotate;
    private bool _target;

    private readonly Dictionary<int, Action<Vector2>> _methods = new();

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, TraumaCVars.MouseWheelZoom, x => _zoom = x, true);
        Subs.CVar(_cfg, TraumaCVars.MouseWheelRotate, x => _rotate = x, true);
        Subs.CVar(_cfg, TraumaCVars.MouseWheelTargeting, x => _target = x, true);
    }

    public override void HandleMouseWheel(Vector2 delta)
    {
        // Get priority of mouse wheel behavior, priority < 0 means behavior cannot be executed
        // Only one behavior can be executed at once, get highest priority one
        // If priority is the same for multiple behavior, order of adding elemets to dict matters
        // Current order: targeting -> rotate -> zoom

        _methods.Clear();
        _methods[GetMouseWheelBehaviorPriority(TraumaKeyFunctions.TargetingMod, _target)] = CycleTargetingBehavior;
        _methods[GetMouseWheelBehaviorPriority(TraumaKeyFunctions.RotateMod, _rotate)] = RotateBehavior;
        _methods[GetMouseWheelBehaviorPriority(TraumaKeyFunctions.ZoomMod, _zoom)] = ZoomBehavior;

        var (priority, method) = _methods.MaxBy(x => x.Key);
        if (priority >= 0)
            method(delta);
    }

    private int GetMouseWheelBehaviorPriority(BoundKeyFunction function, bool cvar)
    {
        if (!cvar)
            return -1;

        if (!_inputMan.TryGetKeyBinding(function, out _))
            return 0;

        return _input.CmdStates.GetState(function) == BoundKeyState.Down ? 1 : -1;
    }

    private void ZoomBehavior(Vector2 delta)
    {
        if (_player.LocalEntity is not { } player || !TryComp(player, out ContentEyeComponent? eye))
            return;

        RaisePredictiveEvent(new SharedContentEyeSystem.RequestTargetZoomEvent
        {
            TargetZoom = eye.TargetZoom - new Vector2(delta.Y * 0.1f) / SharedContentEyeSystem.ZoomMod,
        });
    }

    private void RotateBehavior(Vector2 delta)
    {
        RaisePredictiveEvent(new RotateCameraEvent(delta.Y > 0f ? MathHelper.PiOver2 : -MathHelper.PiOver2));
    }

    private void CycleTargetingBehavior(Vector2 delta)
    {
        _targeting.CycleTargeting(delta.Y > 0f ? 1 : -1);
    }
}
