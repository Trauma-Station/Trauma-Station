// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.LinkAccount;
using Robust.Shared.Player;

namespace Content.Trauma.Client.LinkAccount;

public sealed partial class LinkAccountManager : ILinkAccountManager
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private ISharedPlayerManager _player = default!;

    private readonly List<SharedRMCPatron> _allPatrons = [];

    public SharedRMCPatronTier? Tier { get; set; }
    public bool Linked { get; private set; }
    public Color? GhostColor { get; private set; }
    public SharedRMCLobbyMessage? LobbyMessage { get; private set; }
    public SharedRMCRoundEndShoutouts? RoundEndShoutout { get; private set; }

    public event Action<Guid>? CodeReceived;

    public event Action? Updated;

    private void OnCode(LinkAccountCodeMsg message)
    {
        CodeReceived?.Invoke(message.Code);
    }

    private void OnStatus(LinkAccountStatusMsg ev)
    {
        Tier = ev.Patron?.Tier;
        Linked = ev.Patron?.Linked ?? false;
        GhostColor = ev.Patron?.GhostColor;
        LobbyMessage = ev.Patron?.LobbyMessage;
        RoundEndShoutout = ev.Patron?.RoundEndShoutout;
        Updated?.Invoke();
    }

    private void OnPatronList(RMCPatronListMsg ev)
    {
        _allPatrons.Clear();
        _allPatrons.AddRange(ev.Patrons);
    }

    public IReadOnlyList<SharedRMCPatron> GetPatrons()
        => _allPatrons;

    public bool IsPatron(ICommonSession player)
        => player == _player.LocalSession && IsPatron();

    public bool IsPatron()
#if DEBUG
        => true;
#else
        => Tier != null;
#endif

    public bool CanViewPatronPerks()
        => Tier is { } tier && (tier.GhostColor || tier.LobbyMessage || tier.RoundEndShoutout);

    void IPostInjectInit.PostInject()
    {
        _net.RegisterNetMessage<LinkAccountCodeMsg>(OnCode);
        _net.RegisterNetMessage<LinkAccountRequestMsg>();
        _net.RegisterNetMessage<LinkAccountStatusMsg>(OnStatus);
        _net.RegisterNetMessage<RMCPatronListMsg>(OnPatronList);
        _net.RegisterNetMessage<RMCClearGhostColorMsg>();
        _net.RegisterNetMessage<RMCChangeGhostColorMsg>();
        _net.RegisterNetMessage<RMCChangeLobbyMessageMsg>();
        _net.RegisterNetMessage<RMCChangeNTShoutoutMsg>();
    }
}
