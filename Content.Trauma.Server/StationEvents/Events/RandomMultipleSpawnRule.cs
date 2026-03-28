// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Server.StationEvents.Components;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Random;

namespace Content.Trauma.Server.StationEvents.Events;

public sealed class RandomMultipleSpawnRule : StationEventSystem<RandomMultipleSpawnRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Started(EntityUid uid, RandomMultipleSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        var amount = _random.Next(comp.MinAmount, comp.MaxAmount);
        for (int i = 0; i < amount; i++)
        {
            if (TryFindRandomTile(out _, out _, out _, out var coords))
            {
                Sawmill.Info($"Spawning {comp.Prototype} at {coords}");
                Spawn(comp.Prototype, coords);
            }
        }
    }
}
