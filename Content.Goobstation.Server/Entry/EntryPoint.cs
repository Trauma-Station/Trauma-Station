// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.IoC;
using Content.Goobstation.Common.JoinQueue;
using Content.Goobstation.Common.ServerCurrency;
using Robust.Shared.ContentPack;
using Robust.Shared.Timing;

namespace Content.Goobstation.Server.Entry;

public sealed class EntryPoint : GameServer
{
    [Dependency] private readonly ICommonCurrencyManager _curr = default!;
    [Dependency] private readonly IJoinQueueManager _joinQueue = default!;

    public override void PreInit()
    {
        ServerGoobContentIoC.Register(Dependencies);
    }

    public override void Init()
    {
        base.Init();

        Dependencies.BuildGraph();
        Dependencies.InjectDependencies(this);

        _joinQueue.Initialize();

        _curr.Initialize();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _curr.Shutdown();
    }
}
