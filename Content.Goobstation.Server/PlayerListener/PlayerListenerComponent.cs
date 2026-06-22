// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Server.PlayerListener;

/// <summary>
///     Stores data about players, listens even.
/// </summary>
[RegisterComponent]
public sealed partial class PlayerListenerComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public readonly HashSet<NetUserId> UserIds = [];
}
