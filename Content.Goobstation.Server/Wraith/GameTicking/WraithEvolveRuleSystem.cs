// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Rules;
using Content.Goobstation.Shared.Wraith.GameTicking.Components;

namespace Content.Goobstation.Server.Wraith.GameTicking;

public sealed partial class WraithEvolveRuleSystem : GameRuleSystem<WraithEvolveRuleComponent>
{
    [Dependency] private GameTicker _ticker = default!;

    public void SpawnERTChaplain()
    {
        _ticker.StartGameRule("SpawnERTChaplain");
    }
}
