namespace Content.Shared.Inventory.Events;

public abstract partial class BeforeUnequipEventBase
{
    /// <summary>
    /// True, if item is being forcibly equipped
    /// </summary>
    public readonly bool Force = force;
}
