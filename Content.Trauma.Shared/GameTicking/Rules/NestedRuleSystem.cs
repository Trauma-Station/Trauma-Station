// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;

namespace Content.Trauma.Shared.GameTicking.Rules;

public sealed partial class NestedRuleSystem : GameRuleSystem<NestedRuleComponent>
{
    [Dependency] private EntityTableSystem _entityTable = default!;

    protected override void Started(Entity<NestedRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        var rules = _entityTable.GetSpawns(ent.Comp1.Rules);
        foreach (var rule in rules)
        {
            GameTicker.StartGameRule(rule);
        }
    }
}
