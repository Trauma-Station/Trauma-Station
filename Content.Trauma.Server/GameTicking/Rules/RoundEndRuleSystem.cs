// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Shared.GameTicking.Components;
using Content.Trauma.Server.GameTicking.Rules.Components;

namespace Content.Trauma.Server.GameTicking.Rules;

public sealed partial class RoundEndRuleSystem : GameRuleSystem<RoundEndRuleComponent>
{
    [Dependency] private RoundEndSystem _roundEnd = default!;

    protected override void Started(EntityUid uid, RoundEndRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        _roundEnd.RequestRoundEnd(countdownTime: comp.CountdownTime, checkCooldown: comp.CheckCooldown, cantRecall: comp.CantRecall);
    }
}
