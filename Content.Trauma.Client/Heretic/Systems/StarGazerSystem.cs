// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Cosmos;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Map;

namespace Content.Trauma.Client.Heretic.Systems;

public sealed partial class StarGazerSystem : SharedStarGazerSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IInputManager _input = default!;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!Timing.IsFirstTimePredicted)
            return;

        if (!HasComp<StarGazeComponent>(_player.LocalEntity))
            return;

        var player = _player.LocalEntity.Value;

        MapCoordinates? mousePos = _eye.PixelToMap(_input.MouseScreenPosition);

        if (mousePos.Value.MapId == MapId.Nullspace)
            return;

        RaisePredictiveEvent(new LaserBeamEndpointPositionEvent(GetNetEntity(player), mousePos.Value));
    }
}
