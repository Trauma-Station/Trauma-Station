// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Hands.Systems;
using Content.Trauma.Shared.Multihit;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.Multhit;

public sealed class ActiveMultihitSystem : SharedActiveMultihitSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

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
