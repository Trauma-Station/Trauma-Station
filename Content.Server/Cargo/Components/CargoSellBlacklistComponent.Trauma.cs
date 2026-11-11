namespace Content.Server.Cargo.Components;

public sealed partial class CargoSellBlacklistComponent : Component
{
    /// <summary>
    /// How many direct children are causing this container to be blacklisted for sale.
    /// Used to properly refcount its removal with multiple blacklisted items.
    /// 0 for permanently blacklisted items
    /// </summary>
    [DataField]
    public int BlacklistedChildren;
}
