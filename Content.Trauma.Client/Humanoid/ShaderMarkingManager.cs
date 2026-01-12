using System.Numerics;
using Content.Shared.Humanoid.Markings;
using Content.Trauma.Shared.Humanoid;

namespace Content.Trauma.Client.Humanoid;

public sealed class ShaderMarkingManager
{
    [Dependency] private readonly MarkingManager _markingManager = default!;

    public void Initialize()
    {
        _markingManager.GetMarkingShaderParams += GetMarkingShaderParams;
    }

    public void Shutdown()
    {
        _markingManager.GetMarkingShaderParams -= GetMarkingShaderParams;
    }

    private Dictionary<string, Vector3>? GetMarkingShaderParams(MarkingPrototype proto,
        int index,
        MarkingSet markingSet)
    {
        if (proto.Coloring.Layers is not { } layers)
            return null;

        foreach (var layer in layers.Values)
        {
            if (layer.Type is not ShaderParamsMarkingColoring coloring || coloring.ColorIndex != index)
                continue;

            return coloring.GetParamData(index, markingSet);
        }

        return null;
    }
}
