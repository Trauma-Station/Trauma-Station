// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Random;

namespace Content.Lavaland.Shared.EntityShapes.Shapes;

/// <summary>
/// Returns a singe tile at the specified position.
/// </summary>
public sealed partial class SingleEntityShape : EntityShape
{
    protected override List<Vector2> GetShapeImplementation(IRobustRandom rand, IPrototypeManager proto)
    {
        return new List<Vector2> { Offset };
    }
}
