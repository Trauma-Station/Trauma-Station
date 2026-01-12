using Content.Trauma.Client.Humanoid;

namespace Content.Trauma.Client.IoC;

internal static class ContentTraumaClientIoC
{
    internal static void Register(IDependencyCollection collection)
    {
        collection.Register<ShaderMarkingManager>();
    }
}
