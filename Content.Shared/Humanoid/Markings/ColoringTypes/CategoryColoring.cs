// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 csqrb <56765288+CaptainSqrBeard@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

using System.Linq;

namespace Content.Shared.Humanoid.Markings.ColoringTypes;

/// <summary>
///     Colors marking in color of first defined marking from specified category (in e.x. from Hair category)
/// </summary>
public sealed partial class CategoryColoring : LayerColoringType
{
    [DataField("category", required: true)]
    public HumanoidVisualLayers Category;

    public override Color? GetCleanColor(Color? skin, Color? eyes, List<Marking> otherMarkings)
    {
        Color? outColor = null;

        if (otherMarkings.Count > 0)
        {
            outColor = otherMarkings[0].MarkingColors.FirstOrDefault();
        }

        return outColor;
    }
}
