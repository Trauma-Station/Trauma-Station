using Content.Lavaland.Shared.EntityShapes.Shapes;
using Robust.Shared.Prototypes;

namespace Content.Lavaland.Shared.EntityShapes;

/// <summary>
/// Contains one or multiple EntityShapes to create a pattern.
/// </summary>
[Prototype]
public sealed partial class EntityShapePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public EntityShape Shape = default!;
}
