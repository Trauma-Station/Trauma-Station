using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared.Follower;
using Content.Shared.Follower.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind.Components;
using Content.Trauma.Server.GameTicking.Rules.Components;

namespace Content.Trauma.Server.GameTicking.Rules;

/// <summary>
/// Tracks observer statistics
/// </summary>
public sealed class ObserverStatisticRuleSystem : GameRuleSystem<ObserverStatisticRuleComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StartedFollowingEntityEvent>(OnNewFollow);
    }

    private void OnNewFollow(StartedFollowingEntityEvent ev)
    {
        if (!TryComp<MindContainerComponent>(ev.Following, out var mindContainer))
            return;

        // Only track entities that are actual people
        if (!mindContainer.HasMind)
            return;

        if (!TryComp<FollowedComponent>(ev.Following, out var followed))
            return;

        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var observerStats, out _))
        {
            if (followed.Following.Count <= observerStats.MostPopularEntityPopularity)
                continue;

            observerStats.MostPopularEntity = ev.Following;
            observerStats.MostPopularEntityPopularity = followed.Following.Count;
        }
    }

    protected override void AppendRoundEndText(EntityUid uid,
        ObserverStatisticRuleComponent observerStats,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, observerStats, gameRule, ref args);

        args.AddLine(Loc.GetString("observer-statistic-popularity", ("name", "testjohn"), ("count", 3)));
        // args.AddLine("");
    }
}
