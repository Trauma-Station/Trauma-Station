using Content.Shared.Module;
using Robust.Shared.Network;
using Robust.Shared.Reflection;
using Robust.Shared.Sandboxing;

namespace Content.Shared.Entry;

/// <summary>
/// Trauma - adds module verification helper to fail fast if you mess up building
/// </summary>
public sealed partial class EntryPoint
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IReflectionManager _refMan = default!;
    [Dependency] private readonly ISandboxHelper _sandbox = default!;

    private void VerifyModules()
    {
        var loadedAssemblies = new HashSet<string>(_refMan.Assemblies.Count);
        foreach (var assembly in _refMan.Assemblies)
        {
            if (assembly.GetName().Name is {} name)
                loadedAssemblies.Add(name);
        }

        var missing = new List<string>();
        foreach (var type in _refMan.GetAllChildren<ModulePack>())
        {
            if (_sandbox.CreateInstance(type) is not ModulePack module)
                continue;

            missing.Clear();
            foreach (var req in module.RequiredAssemblies)
            {
                // dont check if required assembly is loaded if its for the other side
                if ((_net.IsClient && req.IsClient || _net.IsServer && req.IsServer) &&
                    !loadedAssemblies.Contains(req.AssemblyName))
                {
                    missing.Add(req.AssemblyName);
                }
            }

            if (missing.Count <= 0)
                continue;

            throw new InvalidOperationException($"Missing required assemblies to build. Try deleting your bin folder, running dotnet clean, and rebuilding the {module.PackName} solution.\nMissing Modules:\n{string.Join("\n", missing)}");
        }
    }
}
