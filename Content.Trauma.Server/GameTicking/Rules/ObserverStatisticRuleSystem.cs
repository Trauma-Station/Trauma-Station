// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared.Follower;
using Content.Shared.Follower.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Trauma.Server.GameTicking.Rules.Components;
using Robust.Server.Player;

namespace Content.Trauma.Server.GameTicking.Rules;

/// <summary>
/// Tracks observer statistics
/// </summary>
public sealed partial class ObserverStatisticRuleSystem : GameRuleSystem<ObserverStatisticRuleComponent>
{
    [Dependency] private IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FollowerComponent, StartedFollowingEntityEvent>(OnNewFollow);
    }

    private void OnNewFollow(Entity<FollowerComponent> ent, ref StartedFollowingEntityEvent ev)
    {
        if (!TryComp<MindContainerComponent>(ev.Following, out var mindContainer))
            return;

        // Only track entities that are actual people
        if (!mindContainer.Mind.HasValue)
            return;

        if (!TryComp<MindComponent>(mindContainer.Mind, out var mindComp))
            return;

        if (mindComp.CharacterName is not { })
            return;

        if (!mindComp.UserId.HasValue)
            return;

        if (!TryComp<FollowedComponent>(ev.Following, out var followed))
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
