// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.SubFloor;
using Content.Trauma.Common.VentCrawling;
using Content.Trauma.Shared.VentCrawling;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.VentCrawling;

public sealed partial class VentCrawlingSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SubFloorHideSystem _subFloorHideSystem = default!;
    [Dependency] private EntityQuery<VentCrawlerComponent> _query = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted ||
            _player.LocalEntity is not { } player ||
            !_query.TryComp(player, out var comp))
            return;

        _subFloorHideSystem.ShowVentPipe = comp.InTube;
    }
}
