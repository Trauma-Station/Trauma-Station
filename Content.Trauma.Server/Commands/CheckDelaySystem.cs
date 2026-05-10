// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Commands;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Commands;

public sealed class CheckDelaySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CheckDelayEvent>(OnCheckDelay);
    }

    private void OnCheckDelay(CheckDelayEvent ev, EntitySessionEventArgs args)
    {
        ev.Received = _timing.RealTime;
        RaiseNetworkEvent(ev, args.SenderSession);
    }
}
