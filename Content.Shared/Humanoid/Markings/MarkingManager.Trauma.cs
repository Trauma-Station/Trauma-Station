using System.Numerics;

namespace Content.Shared.Humanoid.Markings;

public sealed partial class MarkingManager
{
    public Func<MarkingPrototype, int, MarkingSet, Dictionary<string, Vector3>?>? GetMarkingShaderParams;
}
