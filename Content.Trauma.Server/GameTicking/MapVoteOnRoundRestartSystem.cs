// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.GameTicking;
using Content.Server.Voting.Managers;
using Content.Shared.Voting;
using Content.Shared.CCVar;
using Content.Trauma.Common.CCVar;
using Robust.Shared.Configuration;

namespace Content.Trauma.Server.GameTicking;

/// <summary>
/// Starts a map vote at the end of every round, if enabled in the configs
/// </summary>
public sealed partial class MapVoteOnRoundRestartSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IVoteManager _vote = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent args)
    {
        if (!_cfg.GetCVar(TraumaCVars.AutomaticMapVote)
        || !_cfg.GetCVar(CCVars.GameLobbyEnabled)
        || args.New != GameRunLevel.PreRoundLobby) return;
        _vote.CreateStandardVote(null, StandardVoteType.Map);
    }
}
