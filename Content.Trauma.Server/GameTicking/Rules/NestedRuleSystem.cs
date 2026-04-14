// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.GameTicking.Rules;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Content.Trauma.Server.GameTicking.Rules.Components;

namespace Content.Trauma.Server.GameTicking.Rules;

public sealed class NestedRuleSystem : GameRuleSystem<NestedRuleComponent>
{
    [Dependency] private readonly EntityTableSystem _entityTable = default!;

    protected override void Started(EntityUid uid, NestedRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        var rules = _entityTable.GetSpawns(comp.Rules);
        foreach (var rule in rules)
        {
            GameTicker.StartGameRule(rule);
        }
    }
}
