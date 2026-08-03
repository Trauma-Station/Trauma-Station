namespace Content.Server.Store.Conditions;

public sealed partial class BuyBeforeCondition
{
    /// <summary>
    /// If false, only one of the listings needs to be purchased to pass the whitelist.
    /// If true, all of the listings need to be purchased.
    /// </summary>
    [DataField]
    public bool WhitelistRequireAll = true;
}
