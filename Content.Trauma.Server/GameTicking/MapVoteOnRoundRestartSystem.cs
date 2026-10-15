// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Voting.Managers;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Voting;
using Content.Trauma.Common.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;

namespace Content.Trauma.Server.GameTicking;

/// <summary>
/// Starts a map vote at the end of every round, if enabled in the configs
/// </summary>
public sealed partial class MapVoteOnRoundRestartSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IVoteManager _vote = default!;
    [Dependency] private IPlayerManager _player = default!;

    private bool _voteEnabled;
    private bool _lobbyEnabled;
    private int _minPlayers;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, TraumaCVars.AutomaticMapVote, value => _voteEnabled = value, true);
        Subs.CVar(_cfg, TraumaCVars.AutoMapVoteMinPlayers, value => _minPlayers = value, true);
        Subs.CVar(_cfg, CCVars.GameLobbyEnabled, value => _lobbyEnabled = value, true);
    }

    [SubscribeLocalEvent]
    private void OnRunLevelChanged(GameRunLevelChangedEvent args)
    {
        if (!_voteEnabled ||
            !_lobbyEnabled ||
            _player.PlayerCount < _minPlayers ||
            args.New != GameRunLevel.PreRoundLobby)
            return;
        _vote.CreateStandardVote(null, StandardVoteType.Map);
    }
}
