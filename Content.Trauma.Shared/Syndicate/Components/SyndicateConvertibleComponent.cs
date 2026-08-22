using Content.Shared.Materials;

namespace Content.Trauma.Shared.Syndicate.Components;

/// <summary>
/// Indicates that an entity can be converted into the given prototype with a syndicate converter
/// </summary>
[RegisterComponent]
public sealed partial class SyndicateConvertibleComponent : Component
{
    /// <summary>
    /// What the item converts into.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId? ConvertTo;

    /// <summary>
    /// How much the item increases the converter's alertness value.
    /// </summary>
    [DataField]
    public int AlertValue;

    /// <summary>
    /// How long the conversion takes to complete.
    /// </summary>
    [DataField]
    public TimeSpan ConversionTime = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Materials needed to convert the item.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<MaterialPrototype>, int> MaterialCost = new();
}
