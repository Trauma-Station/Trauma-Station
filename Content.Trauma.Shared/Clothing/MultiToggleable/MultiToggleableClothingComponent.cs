// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Trauma.Common.Inventory;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.Clothing;

/// <summary>
/// <see cref="ToggleableClothingComponent"/> but supports multiple attached clothing and overslotting
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MultiToggleableClothingComponent : Component
{
    public const string DefaultClothingContainerId = "multi-toggleable-clothing";

    /// <summary>
    /// Dictionary of inventory slots and entity prototypes to spawn into the clothing container.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<InventorySlotPrototype>, EntProtoId> ClothingPrototypes = new();

    /// <summary>
    /// Dictionary of clothing uids and slots
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, ProtoId<InventorySlotPrototype>> ClothingUids = new();

    /// <summary>
    /// The inventory slot flags required for this component to function.
    /// </summary>
    [DataField]
    public SlotFlags RequiredFlags = SlotFlags.BACK;

    /// <summary>
    /// If true all attached will replace already equipped clothing on equip attempt
    /// </summary>
    [DataField]
    public bool ReplaceCurrentClothing;

    /// <summary>
    /// Time it takes for this clothing to be toggled via the stripping menu verbs. Null prevents the verb from even showing up.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? StripDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// The container that the clothing is stored in when not equipped.
    /// </summary>
    [DataField]
    public string ContainerId = DefaultClothingContainerId;

    [ViewVariables]
    public Container Container = default!;
}
