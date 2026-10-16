// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Voting.Managers;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Voting;
using Content.Trauma.Common.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.GameTicking;

/// <summary>
/// Starts a map vote at the end of every round, if enabled in the configs
/// </summary>
public sealed partial class MapVoteOnRoundRestartSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IVoteManager _vote = default!;
    [Dependency] private IGameTiming _timing = default!;

    private bool _voteEnabled;
    private bool _lobbyEnabled;
    private TimeSpan _delayOnRestart;
    private TimeSpan? _voteDelay;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, TraumaCVars.AutomaticMapVote, value => _voteEnabled = value, true);
        Subs.CVar(_cfg, TraumaCVars.AutoMapVoteDelay, value => _delayOnRestart = value, true);
        Subs.CVar(_cfg, CCVars.GameLobbyEnabled, value => _lobbyEnabled = value, true);
    }

    [SubscribeLocalEvent]
    private void OnRunLevelChanged(GameRunLevelChangedEvent args)
    {
        if (!_voteEnabled ||
            !_lobbyEnabled ||
            args.New != GameRunLevel.PreRoundLobby)
            return;

        // If the server has just started, delay the vote.
        if (_timing.CurTime < TimeSpan.FromMinutes(2))
        {
            _voteDelay = _timing.CurTime + _delayOnRestart;
            return;
        }
        _vote.CreateStandardVote(null, StandardVoteType.Map);
    }

    public override void Update (float frameTime)
    {
        if (_voteDelay is not { } delay || _timing.CurTime < delay) return;

        _vote.CreateStandardVote(null, StandardVoteType.Map);
        _voteDelay = null;
    }
}
