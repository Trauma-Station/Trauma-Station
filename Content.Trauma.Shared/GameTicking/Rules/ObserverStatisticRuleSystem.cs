// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Follower;
using Content.Shared.Follower.Components;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Events;
using Content.Shared.GameTicking.Rules;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Trauma.Shared.GameTicking.Rules;

/// <summary>
/// Tracks observer statistics
/// </summary>
public sealed partial class ObserverStatisticRuleSystem : GameRuleSystem<ObserverStatisticRuleComponent>
{
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private EntityQuery<FollowedComponent> _followedQuery = default!;

    private const string Rule = "SpectatorStatistics";

    [SubscribeLocalEvent]
    private void OnNewFollow(Entity<FollowerComponent> ent, ref StartedFollowingEntityEvent ev)
    {
        if (!_mind.TryGetMind(ev.Following, out var mind, out var mindComp) ||
            mindComp.CharacterName is not { } ||
            mindComp.UserId is not { } id ||
            !_followedQuery.TryComp(ev.Following, out var followed))
            return;

        var query = QueryActiveRules();
        var count = followed.Following.Count;
        while (query.MoveNext(out _, out var rule, out _, out _))
        {
            if (count <= rule.MostPopularEntityPopularity)
                continue;

            rule.MostPopularCharacterName = mindComp.CharacterName;
            rule.MostPopularUserName = _player.GetPlayerData(id).UserName;

            rule.MostPopularEntityPopularity = count;
        }
    }

    [SubscribeLocalEvent]
    private void OnRoundStarting(RoundStartingEvent ev)
    {
        GameTicker.StartGameRule(Rule);
    }

    protected override void AppendRoundEndText(Entity<ObserverStatisticRuleComponent> ent, ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(ent, ref args);

        args.AddLine("");
        args.AddLine(Loc.GetString("observer-statistic-popularity",
            ("name", ent.Comp.MostPopularCharacterName),
            ("username", ent.Comp.MostPopularUserName),
            ("count", ent.Comp.MostPopularEntityPopularity)));
    }
}
