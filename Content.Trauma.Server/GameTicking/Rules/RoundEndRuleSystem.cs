// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.RoundEnd;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Content.Trauma.Shared.GameTicking.Rules;

namespace Content.Trauma.Server.GameTicking.Rules;

public sealed partial class RoundEndRuleSystem : GameRuleSystem<RoundEndRuleComponent>
{
    [Dependency] private RoundEndSystem _roundEnd = default!;

    protected override void Started(Entity<RoundEndRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        var comp = ent.Comp1;
        _roundEnd.RequestRoundEnd(countdownTime: comp.CountdownTime, checkCooldown: comp.CheckCooldown, cantRecall: comp.CantRecall);
    }
}
