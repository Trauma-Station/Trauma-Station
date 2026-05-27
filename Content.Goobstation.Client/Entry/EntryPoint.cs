// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Client.IoC;
using Content.Goobstation.Client.Polls;
using Content.Goobstation.Client.JoinQueue;
using Content.Goobstation.Common.ServerCurrency;
using Robust.Shared.ContentPack;
using Robust.Shared.Timing;

namespace Content.Goobstation.Client.Entry;

public sealed partial class EntryPoint : GameClient
{
    [Dependency] private JoinQueueManager _joinQueue = default!;
    [Dependency] private PollManager _pollManager = default!;
    [Dependency] private ICommonCurrencyManager _currMan = default!;

    public override void PreInit()
    {
        ContentGoobClientIoC.Register(Dependencies);
    }

    public override void Init()
    {
        Dependencies.BuildGraph();
        Dependencies.InjectDependencies(this);
    }

    public override void PostInit()
    {
        base.PostInit();

        _joinQueue.Initialize();
        _pollManager.Initialize();
        _currMan.Initialize();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _currMan.Shutdown();
    }
}
