// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.IoC;
using Content.Goobstation.Common.ServerCurrency;
using Robust.Shared.ContentPack;
using Robust.Shared.Timing;

namespace Content.Goobstation.Server.Entry;

public sealed partial class EntryPoint : GameServer
{
    [Dependency] private ICommonCurrencyManager _curr = default!;

    public override void PreInit()
    {
        ServerGoobContentIoC.Register(Dependencies);
    }

    public override void Init()
    {
        base.Init();

        Dependencies.BuildGraph();
        Dependencies.InjectDependencies(this);

        _curr.Initialize();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _curr.Shutdown();
    }
}
