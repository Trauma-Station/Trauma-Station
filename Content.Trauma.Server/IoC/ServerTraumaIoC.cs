// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.LinkAccount;
using Content.Trauma.Server.LinkAccount;
using Content.Trauma.Server.Mentor;

namespace Content.Trauma.Server.IoC;

public static class ServerTraumaIoC
{
    public static void Register(IDependencyCollection collection)
    {
        collection.Register<MentorManager>();
        collection.Register<ILinkAccountManager, LinkAccountManager>();
        collection.Register<LinkAccountManager>();
    }
}
