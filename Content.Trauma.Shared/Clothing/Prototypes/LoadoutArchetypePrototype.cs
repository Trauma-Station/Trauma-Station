// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Roles;
using Content.Trauma.Common.Inventory;

namespace Content.Trauma.Shared.Clothing.Prototypes;

/// <summary>
/// Prototype that defines a loadout for <see cref="RandomLoadoutPrototype"/>, it's pools and optionally, it's starting gear.
/// </summary>
[Prototype]
public sealed partial class LoadoutArchetypePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Optional starting gear.
    /// </summary>
    [DataField]
    public ProtoId<StartingGearPrototype>? Gear;

    /// <summary>
    /// Loadout pool per inventory slot.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<InventorySlotPrototype>, LoadoutSlotPool> Slots = new();

    /// <summary>
    /// Loadout pools for items held in hands.
    /// </summary>
    [DataField]
    public List<LoadoutSlotPool> Hands = new();
}

/// <summary>
/// A loadout pool.
/// </summary>
/// <param name="Items">A weighted dictionary of <see cref="EntProtoId"/></param>
/// <param name="Chance">The chance of this pool to roll</param>
[DataRecord]
public sealed partial record LoadoutSlotPool(Dictionary<EntProtoId, float> Items, float Chance = 1f);
