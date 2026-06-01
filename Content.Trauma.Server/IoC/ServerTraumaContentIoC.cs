// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Server.Mentor;

namespace Content.Trauma.Server.IoC;

internal static class ServerTraumaContentIoC
{
    internal static void Register(IDependencyCollection collection)
    {
        collection.Register<MentorManager>();
    }
}
