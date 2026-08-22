// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Server.IoC;
using Robust.Shared.ContentPack;

namespace Content.Trauma.Server.Entry;

public sealed class EntryPoint : GameServer
{
    public override void PreInit()
    {
        ServerTraumaIoC.Register(Dependencies);
    }
}
