// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shitcode.Common.LinkAccount;

public interface ILinkAccountManager
{
    IReadOnlyList<SharedRMCPatron> GetPatrons();
    bool CanViewPatronPerks();
    public SharedRMCPatronTier? Tier { get; set; }
}
