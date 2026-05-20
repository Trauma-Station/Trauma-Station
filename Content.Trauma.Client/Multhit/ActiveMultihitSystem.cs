// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Hands.Systems;
using Content.Trauma.Shared.Multihit;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.Multhit;

public sealed partial class ActiveMultihitSystem : SharedActiveMultihitSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IInputManager _input = default!;
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        if (_player.LocalEntity is not { } player)
            return;

        var mousePos = _eye.PixelToMap(_input.MouseScreenPosition);

        var coords = _transform.GetMapCoordinates(player);

        if (mousePos.MapId != coords.MapId)
            return;

        foreach (var held in _hands.EnumerateHeld(player))
        {
            if (!ActiveQuery.HasComp(held))
                continue;

            RaisePredictiveEvent(
                new UpdateMultihitDirectionEvent(GetNetEntity(held), mousePos.Position - coords.Position));
        }
    }
}
