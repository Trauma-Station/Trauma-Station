using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;

namespace Content.Client.Humanoid;

/// <summary>
/// Trauma - store previous colors for each layer too so switching hair styles keeps the color.
/// </summary>
public sealed partial class MarkingsViewModel
{
    private Dictionary<HumanoidVisualLayers, List<Color>> _previousLayerColors = new();

    public List<Color>? GetPreviousColors(HumanoidVisualLayers layer, MarkingPrototype marking)
    {
        if (!_previousLayerColors.TryGetValue(layer, out var colors))
            return null;

        if (colors.Count != marking.Sprites.Count)
            return null; // not compatible with this marking, can't use it

        return colors;
    }
}
