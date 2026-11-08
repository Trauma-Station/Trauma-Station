using Content.Shared.Roles;

namespace Content.Trauma.Shared.Clothing;

/// <summary>
/// A role's possible outfits (ex: Bartender, Chef). One is rolled per spawn
/// </summary>
[Prototype]
public sealed partial class RandomLoadoutPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Outfits by name. One is picked per spawn
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, LoadoutArchetype> Archetypes = new();
}

[DataDefinition]
public sealed partial class LoadoutArchetype
{
    /// <summary>
    /// Odds of being picked, relative to the other archetypes
    /// </summary>
    [DataField]
    public float Weight = 1f;

    /// <summary>
    /// Fixed gear equipped first (storage, inhand items)
    /// </summary>
    [DataField]
    public ProtoId<StartingGearPrototype>? Gear;

    /// <summary>
    /// Slot name to item pool. Filled slots are skipped.
    /// </summary>
    [DataField]
    public Dictionary<string, LoadoutSlotPool> Slots = new();
}

[DataDefinition]
public sealed partial class LoadoutSlotPool
{
    /// <summary>
    /// 0-1 chance this slot gets filled at all
    /// </summary>
    [DataField]
    public float Chance = 1f;

    /// <summary>
    /// Item ID to weight.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<EntProtoId, float> Items = new();
}
