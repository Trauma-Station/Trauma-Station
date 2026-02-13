// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Trauma.Client.IoC;
using Content.Trauma.Client.Knowledge;
using Content.Trauma.Common.Knowledge;
using Robust.Shared.ContentPack;

namespace Content.Trauma.Client.Entry;

public sealed class EntryPoint : GameClient
{
    public override void PreInit()
    {
        base.PreInit();

        ContentTraumaClientIoC.Register(Dependencies);
    }
}
