namespace Content.Shitcode.Common.LinkAccount;

public interface ILinkAccountManager
{
    IReadOnlyList<SharedRMCPatron> GetPatrons();
    bool CanViewPatronPerks();
    public SharedRMCPatronTier? Tier { get; set; }
}
