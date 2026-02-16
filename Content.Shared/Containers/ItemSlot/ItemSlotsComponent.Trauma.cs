namespace Content.Shared.Containers.ItemSlots;

/// <summary>
/// Trauma - add SmartEquip bool
/// </summary>
public sealed partial class ItemSlotsComponent
{
    /// <summary>
    /// Allows smart equip keybinds (e.g. F for suit storage) to take and insert items.
    /// The default of false will just put the item itself into your hands.
    /// </summary>
    [DataField]
    public bool SmartEquip;
}

/// <summary>
/// Trauma - add lavaland related fields for a slot
/// </summary>
public sealed partial class ItemSlot
{
    /// <summary>
    ///     Lavaland Change: Can light go through the container of this ItemSlot?
    /// </summary>
    [DataField]
    [Access(typeof(ItemSlotsSystem), Other = AccessPermissions.ReadWriteExecute)]
    public bool OccludesLight = true;

    /// <summary>
    ///     Lavaland Change: if specified, doesn't insert the item instantly, but instead after a do-after passes.
    /// </summary>
    [DataField]
    [Access(typeof(ItemSlotsSystem), Other = AccessPermissions.ReadWriteExecute)]
    public TimeSpan? InsertDelay;

    /// <summary>
    ///     Lavaland Change: if specified, doesn't remove the item instantly, but instead after a do-after passes.
    /// </summary>
    [DataField]
    [Access(typeof(ItemSlotsSystem), Other = AccessPermissions.ReadWriteExecute)]
    public TimeSpan? EjectDelay;
}
