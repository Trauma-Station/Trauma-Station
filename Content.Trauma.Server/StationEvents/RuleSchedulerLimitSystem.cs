// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.GameTicking;
using Content.Trauma.Common.StationEvents;

namespace Content.Trauma.Server.StationEvents;

public sealed partial class RuleSchedulerLimitSystem : EntitySystem
{
    [Dependency] private GameTicker _ticker = default!;

    [SubscribeLocalEvent]
    private void OnRuleScheduled(Entity<RuleSchedulerLimitComponent> ent, ref RuleScheduledEvent args)
    {
        ent.Comp.Count++;
        if (ent.Comp.Count < ent.Comp.Limit)
            return;

        Log.Info($"Stopping scheduler {ToPrettyString(ent)} as it has reached its limit of {ent.Comp.Limit} rules");
        _ticker.EndGameRule(ent.Owner);
    }
}
