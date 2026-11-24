namespace Content.Shared.Inventory.Events;

public abstract partial class BeforeUnequipEventBase
{
    /// <summary>
    /// True, if item is being forcibly unequipped
    /// </summary>
    public bool Force;
}
