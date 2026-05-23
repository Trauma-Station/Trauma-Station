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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var player = _player.LocalPlayer?.ControlledEntity;

        var ventCrawlerQuery = GetEntityQuery<VentCrawlerComponent>();

        if (!ventCrawlerQuery.TryGetComponent(player, out var playerVentCrawlerComponent))
            return;

        _subFloorHideSystem.ShowVentPipe = playerVentCrawlerComponent.InTube;
    }
}
