// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.LinkAccount;

namespace Content.Trauma.Client.LinkAccount;

public sealed class LinkAccountSystem : EntitySystem
{
    public event Action<SharedRMCDisplayLobbyMessageEvent>? LobbyMessageReceived;

    public override void Initialize()
    {
        SubscribeNetworkEvent<SharedRMCDisplayLobbyMessageEvent>(OnDisplayLobbyMessage);
    }

    private void OnDisplayLobbyMessage(SharedRMCDisplayLobbyMessageEvent ev)
    {
        LobbyMessageReceived?.Invoke(ev);
    }
}
