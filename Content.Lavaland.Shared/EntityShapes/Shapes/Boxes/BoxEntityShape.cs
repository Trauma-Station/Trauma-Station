// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Robust.Shared.Prototypes;

namespace Content.Lavaland.Shared.EntityShapes.Shapes;

public sealed partial class BoxEntityShape : EntityShape
{
    [DataField]
    public bool Hollow;

    protected override List<Vector2> GetShapeImplementation(System.Random rand, IPrototypeManager proto)
    {
        return ShapeHelpers.MakeBox(Offset, Size, Hollow, StepSize).ToList();
    }
}
