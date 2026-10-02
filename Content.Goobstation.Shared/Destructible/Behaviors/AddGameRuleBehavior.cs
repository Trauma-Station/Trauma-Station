// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.SlaughterDemon.Items;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Shared.GameTicking;

namespace Content.Goobstation.Shared.Destructible.Behaviors;

[DataDefinition]
public sealed partial class AddGameRuleBehavior : IThresholdBehavior
{
    [DataField(required: true)]
    public EntProtoId Rule;

    public void Execute(EntityUid owner, SharedDestructibleSystem system, EntityUid? cause = null)
    {
        if (system.EntityManager.TryGetComponent<VialSummonComponent>(owner, out var comp))
            comp.Summoner = cause;

        system.EntityManager.System<GameTicker>().StartGameRule(Rule);
    }
}
