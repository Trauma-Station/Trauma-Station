// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Physics.ComplexJoint;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Map;

namespace Content.Trauma.Client.Physics;

public sealed partial class ContinuousBeamSystem : SharedContinuousBeamSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IInputManager _input = default!;
    [Dependency] private InputSystem _inputSystem = default!;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!Timing.IsFirstTimePredicted)
            return;

        if (_player.LocalEntity is not { } player)
            return;

        if (!TryGetGun(player, out var gun))
            return;

        MapCoordinates? mousePos = _eye.PixelToMap(_input.MouseScreenPosition);

        if (mousePos.Value.MapId == MapId.Nullspace)
            return;

        var keyFunc = gun.Value.Comp.AltFire ? EngineKeyFunctions.UseSecondary : EngineKeyFunctions.Use;
        var requestFire = CanFire(player, gun.Value) && _inputSystem.CmdStates.GetState(keyFunc) == BoundKeyState.Down;

        RaisePredictiveEvent(new LaserBeamEndpointPositionEvent(GetNetEntity(gun.Value), mousePos.Value, requestFire));
    }
}
