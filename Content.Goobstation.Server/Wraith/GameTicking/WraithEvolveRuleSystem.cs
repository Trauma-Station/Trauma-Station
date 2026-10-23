// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.GameTicking.Rules;
using Content.Goobstation.Shared.Wraith.GameTicking.Components;

namespace Content.Goobstation.Server.Wraith.GameTicking;

public sealed partial class WraithEvolveRuleSystem : GameRuleSystem<WraithEvolveRuleComponent>
{
    private static readonly EntProtoId ERTChaplainEvent = "SpawnERTChaplain";

    public void SpawnERTChaplain()
    {
        GameTicker.StartGameRule(ERTChaplainEvent);
    }
}
