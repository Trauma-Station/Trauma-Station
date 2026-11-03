// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.ServerCurrency;
using Content.Goobstation.Server.JumpScare;
using Content.Goobstation.Server.Polls;
using Content.Goobstation.Server.Redial;
using Content.Goobstation.Server.ServerCurrency;
using Content.Goobstation.Shared.JumpScare;

namespace Content.Goobstation.Server.IoC;

internal static class ServerGoobContentIoC
{
    internal static void Register(IDependencyCollection instance)
    {
        instance.Register<RedialManager>();
        instance.Register<PollManager>();
        instance.Register<IFullScreenImageJumpscare, ServerFullScreenImageJumpscare>();
        instance.Register<ICommonCurrencyManager, ServerCurrencyManager>();
    }
}
