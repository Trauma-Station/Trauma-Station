// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.GameTicking;
using Content.Trauma.Common.StationEvents;

namespace Content.Trauma.Server.StationEvents;

public sealed class RuleSchedulerLimitSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _ticker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RuleSchedulerLimitComponent, RuleScheduledEvent>(OnRuleScheduled);
    }

    private void OnRuleScheduled(Entity<RuleSchedulerLimitComponent> ent, ref RuleScheduledEvent args)
    {
        ent.Comp.Count++;
        if (ent.Comp.Count < ent.Comp.Limit)
            return;

        Log.Info($"Stopping scheduler {ToPrettyString(ent)} as it has reached its limit of {ent.Comp.Limit} rules");
        _ticker.EndGameRule(ent.Owner);
    }
}
