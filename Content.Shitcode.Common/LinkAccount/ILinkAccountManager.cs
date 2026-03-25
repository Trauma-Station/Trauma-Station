namespace Content.Shitcode.Common.LinkAccount;

public interface ILinkAccountManager
{
    IReadOnlyList<SharedRMCPatron> GetPatrons();
}
