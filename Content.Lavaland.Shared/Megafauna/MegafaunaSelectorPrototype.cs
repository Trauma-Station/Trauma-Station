using Content.Lavaland.Shared.Megafauna.Selectors;
using Robust.Shared.Prototypes;

namespace Content.Lavaland.Shared.Megafauna;

/// <summary>
/// Contains one or multiple EntityShapes to create a pattern.
/// </summary>
[Prototype]
public sealed partial class MegafaunaSelectorPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public MegafaunaSelector Selector = default!;
}
