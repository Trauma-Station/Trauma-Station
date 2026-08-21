// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Shared.Follower;
using Content.Shared.Follower.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Trauma.Server.GameTicking.Rules.Components;
using Robust.Server.Player;
using Robust.Shared.Map;

namespace Content.Trauma.Server.GameTicking.Rules;

/// <summary>
/// Tracks observer statistics
/// </summary>
public sealed partial class ObserverStatisticRuleSystem : GameRuleSystem<ObserverStatisticRuleComponent>
{
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private EntityQuery<FollowedComponent> _followedQuery = default!;

    private const string Rule = "SpectatorStatistics";

    [SubscribeLocalEvent]
    private void OnNewFollow(Entity<FollowerComponent> ent, ref StartedFollowingEntityEvent ev)
    {
        if (!_mind.TryGetMind(ev.Following, out var mind, out var mindComp))
            return;

        if (mindComp.CharacterName is not { })
            return;

        if (mindComp.UserId is not { })
            return;

        if (!_followedQuery.TryComp(ev.Following, out var followed))
            return;

        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var observerStats, out _))
        {
            if (followed.Following.Count <= observerStats.MostPopularEntityPopularity)
                continue;

            observerStats.MostPopularCharacterName = mindComp.CharacterName;
            observerStats.MostPopularUserName = _playerManager.GetPlayerData(mindComp.UserId.Value).UserName;

            observerStats.MostPopularEntityPopularity = followed.Following.Count;
        }
    }

    [SubscribeLocalEvent]
    private void OnRoundStarting(RoundStartingEvent ev)
    {
        var rule = Spawn(Rule, MapCoordinates.Nullspace);
        _ticker.StartGameRule(rule);
    }

    protected override void AppendRoundEndText(
        EntityUid uid,
        ObserverStatisticRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        args.AddLine("");
        args.AddLine(Loc.GetString("observer-statistic-popularity", ("name", component.MostPopularCharacterName), ("username", component.MostPopularUserName), ("count", component.MostPopularEntityPopularity)));
    }
}
