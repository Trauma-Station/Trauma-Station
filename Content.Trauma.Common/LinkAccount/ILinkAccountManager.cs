// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Player;

namespace Content.Trauma.Common.LinkAccount;

public interface ILinkAccountManager : IPostInjectInit
{
    /// <summary>
    /// Get a list of all patrons.
    /// </summary>
    public IReadOnlyList<SharedRMCPatron> GetPatrons();

    /// <summary>
    /// Returns true if a given player is a patron.
    /// On the client, always returns false for other players.
    /// </summary>
    public bool IsPatron(ICommonSession player);

    /// <summary>
    /// Returns true if the local entity is a patron.
    /// On the server, always returns false.
    /// </summary>
    public bool IsPatron();

    /// <summary>
    /// Returns true if the local entity is able to view the patron perks menu.
    /// On the server, always returns false.
    /// </summary>
    public bool CanViewPatronPerks();
}
