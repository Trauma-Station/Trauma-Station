// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Client.LinkAccount;
using Content.Trauma.Common.LinkAccount;

namespace Content.Trauma.Client.IoC;

internal static class ContentTraumaClientIoC
{
    internal static void Register(IDependencyCollection collection)
    {
        collection.Register<ILinkAccountManager, LinkAccountManager>();
        collection.Register<LinkAccountManager>();
    }
}
