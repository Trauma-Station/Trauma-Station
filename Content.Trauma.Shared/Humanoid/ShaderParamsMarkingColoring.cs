using System.Linq;
using System.Numerics;
using Content.Shared.Humanoid.Markings;

namespace Content.Trauma.Shared.Humanoid;

public sealed partial class ShaderParamsMarkingColoring : MarkingColoring
{
    // Param name -> (min, max)
    [DataField(required: true)]
    public Dictionary<string, Vector2> Params = new();

    // returns param name -> (min, max, current)
    public Dictionary<string, Vector3>? GetParamData(int index, MarkingSet markingSet)
    {
        if (GetRgba(markingSet) is not { } col)
            return null;

        var ordered = Params.OrderBy(x => x.Key).ToList();

        var result = new Dictionary<string, Vector3>();

        for (var i = 0; i < ordered.Count; i++)
        {
            var cur = ordered[i];
            result[cur.Key] = new Vector3(cur.Value, col[i]);
        }

        return result;
    }

    public override Color? GetCleanColor(Color? skin, Color? eyes, MarkingSet markingSet)
    {
        if (GetRgba(markingSet) is not { } col)
            return null;

        var result = new Vector4();

        var ordered = Params.OrderBy(x => x.Key).Select(x => x.Value).ToList();

        for (var i = 0; i < ordered.Count; i++)
        {
            result[i] = Math.Clamp(col[i], ordered[i].X, ordered[i].Y);
        }

        return new Color(result);
    }

    private Vector4? GetRgba(MarkingSet markingSet)
    {
        if (Params.Count is < 0 or > 4)
            return null;

        return markingSet.Markings.GetValueOrDefault(MarkingCategory)
            ?.FirstOrDefault(x => x.MarkingId == MarkingId)
            ?.MarkingColors.ElementAtOrDefault(ColorIndex)
            .RGBA;
    }
}
