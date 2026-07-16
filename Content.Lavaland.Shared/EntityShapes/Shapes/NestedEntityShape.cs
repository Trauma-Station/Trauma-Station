// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Random;

namespace Content.Lavaland.Shared.EntityShapes.Shapes;

/// <summary>
/// Shape that references a ProtoId containing some other shape.
/// </summary>
public sealed partial class NestedEntityShape : EntityShape
{
    [DataField(required: true)]
    public ProtoId<EntityShapePrototype> Id;

    protected override List<Vector2> GetShapeImplementation(IRobustRandom rand, IPrototypeManager proto)
    {
        return proto.Index(Id).Shape.GetShape(rand, proto, Offset, Size, StepSize);
    }
}
