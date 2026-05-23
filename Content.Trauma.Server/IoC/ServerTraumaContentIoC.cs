using Content.Trauma.Server.Mentor;

namespace Content.Trauma.Server.IoC;

internal static class ServerTraumaContentIoC
{
    internal static void Register(IDependencyCollection collection)
    {
        collection.Register<MentorManager>();
    }
}
