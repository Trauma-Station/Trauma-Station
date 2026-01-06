// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 Sara Aldrete's Top Guy <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.IoC;
using Content.Goobstation.Server.Voice;
using Content.Goobstation.Common.JoinQueue;
using Content.Goobstation.Common.ServerCurrency;
using Robust.Shared.ContentPack;
using Robust.Shared.Timing;

namespace Content.Goobstation.Server.Entry;

public sealed class EntryPoint : GameServer
{
    [Dependency] private readonly ICommonCurrencyManager _curr = default!;
    [Dependency] private readonly IVoiceChatServerManager _voiceManager = default!;
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

    public override void Update(ModUpdateLevel level, FrameEventArgs frameEventArgs)
    {
        base.Update(level, frameEventArgs);

        switch (level)
        {
            case ModUpdateLevel.PreEngine:
                _voiceManager.Update();
                break;

        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _curr.Shutdown();
        _voiceManager.Shutdown();
    }
}
