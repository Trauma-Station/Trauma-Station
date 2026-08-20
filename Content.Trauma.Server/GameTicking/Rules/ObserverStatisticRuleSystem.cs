using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared.Follower;
using Content.Shared.Follower.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind.Components;
using Content.Trauma.Common.Mind;
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

        SubscribeLocalEvent<FollowerComponent, StartedFollowingEntityEvent>(OnNewFollow);
    }

    private void OnNewFollow(Entity<FollowerComponent> ent, ref StartedFollowingEntityEvent ev)
    {
        if (!TryComp<MindContainerComponent>(ev.Following, out var mindContainer))
            return;

        // Only track entities that are actual people
        if (!mindContainer.HasMind)
            return;

        TryComp(ev.Following, out MetaDataComponent? meta);
        if (meta is not { })
            return;

        if (!TryComp<FollowedComponent>(ev.Following, out var followed))
            return;

        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var observerStats, out _))
        {
            if (followed.Following.Count <= observerStats.MostPopularEntityPopularity)
                continue;

            //_roles.MindGetAllRoleInfo(mindId);
            observerStats.MostPopular = meta.EntityName;
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

        Log.Debug("Most Popular");
        Log.Debug(component.MostPopular);
        Log.Debug(component.MostPopularEntityPopularity.ToString());
        args.AddLine("");
        args.AddLine(Loc.GetString("observer-statistic-popularity", ("name", component.MostPopular), ("count", component.MostPopularEntityPopularity)));
    }
}
