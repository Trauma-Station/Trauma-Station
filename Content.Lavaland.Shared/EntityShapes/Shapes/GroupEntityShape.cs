// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Random.Helpers;
using Robust.Shared.Random;

namespace Content.Lavaland.Shared.EntityShapes.Shapes;

/// <summary>
/// Picks one shape out of a list of children using weights to randomize between them.
/// </summary>
public sealed partial class GroupEntityShape : EntityShape
{
    [DataField(required: true)]
    public List<EntityShape> Children = new();

    protected override List<Vector2> GetShapeImplementation(IRobustRandom rand, IPrototypeManager proto)
    {
        var children = new Dictionary<EntityShape, float>(Children.Count);
        foreach (var child in Children)
        {
            children.Add(child, child.Weight);
        }

        if (children.Count == 0)
            return new List<Vector2>();

        var pick = SharedRandomExtensions.Pick(children, rand);
        return pick.GetShape(rand, proto, Offset, Size, StepSize);
    }
}
