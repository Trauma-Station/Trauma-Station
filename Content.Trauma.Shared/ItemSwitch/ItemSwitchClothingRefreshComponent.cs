using Content.Shared.Inventory;

namespace Content.Trauma.Shared.ItemSwitch;

/// <summary>
/// Add to clothing that uses ItemSwitch to grant components to the wearer via ClothingGrantComponentComponent
/// Tracks what was granted so it can be cleaned up when the state changes
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ItemSwitchClothingRefreshComponent : Component
{
    /// <summary>
    /// The slot flags to check when looking for the wearer. Should match the clothing's slot
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public SlotFlags TargetSlots = SlotFlags.HEAD;

    /// <summary>
    /// The wearer entity that components were granted to
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public EntityUid? Wearer;

    /// <summary>
    /// The component type names that were granted to the wearer
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public HashSet<string> GrantedComponents = new();
}
