// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.GameTicking;
using Content.Trauma.Shared.Antag;

namespace Content.Trauma.Server.Antag;

public sealed class AntagSummonerSystem : SharedAntagSummonerSystem
{
    [Dependency] private readonly GameTicker _ticker = default!;

    protected override void SummonAntag(Entity<AntagSummonerComponent> ent)
    {
        _ticker.StartGameRule(ent.Comp.GameRule);
    }
}
