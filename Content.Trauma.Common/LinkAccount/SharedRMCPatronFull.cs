// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.LinkAccount;
using Robust.Shared.Serialization;

namespace Content.Trauma.Common.LinkAccount;

[Serializable, NetSerializable]
public sealed record SharedRMCPatronFull(
    SharedRMCPatronTier? Tier,
    bool Linked,
    Color? GhostColor,
    SharedRMCLobbyMessage? LobbyMessage,
    SharedRMCRoundEndShoutouts? RoundEndShoutout
);
