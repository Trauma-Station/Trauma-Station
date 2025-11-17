using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// A prototype for entity effects which can be reused via <see cref="NestedEffect"/>.
/// </summary>
[Prototype]
public sealed partial class EntityEffectPrototype: IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// The effects of this prototype.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;
}
