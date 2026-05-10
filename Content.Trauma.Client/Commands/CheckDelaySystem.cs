// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Commands;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.Commands;

public sealed class CheckDelaySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private bool _waiting; // no malf server / duplicate packets spamming console

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CheckDelayEvent>(OnCheckDelay);
    }

    private void OnCheckDelay(CheckDelayEvent args)
    {
        if (!_waiting)
            return;

        _waiting = false;
        var now = _timing.ServerTime;
        var c2s = args.Received - args.Sent;
        var s2c = now - args.Received;
        var ping = c2s + s2c;
        Log.Info($"Client->Server: {c2s.TotalMilliseconds} ms");
        Log.Info($"Server->Client: {s2c.TotalMilliseconds} ms");
        Log.Info($"Total ping: {ping.TotalMilliseconds} ms");
    }

    public void CheckDelay()
    {
        if (_waiting)
            Log.Warning("Already waiting for response");

        Log.Info("Checking delay to server...");
        _waiting = true;
        RaiseNetworkEvent(new CheckDelayEvent(_timing.ServerTime));
    }
}
