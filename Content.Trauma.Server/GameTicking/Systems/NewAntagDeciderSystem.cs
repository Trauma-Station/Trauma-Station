// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.GameTicking;
using Content.Server.RoundEnd;
using Content.Shared.Objectives.Systems;
using Content.Trauma.Common.GameTicking;

namespace Content.Trauma.Server.GameTicking.Systems;


public sealed partial class NewAntagDeciderSystem : CommonRequestNewAntagOrCallEvacSystem
{
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private TargetSystem _target = default!;
    [Dependency] private RoundEndSystem _roundEndSystem = default!;

    public override void SpawnNewAntagIfBelowPercent(float percent, int aliveOnSpawn, TimeSpan countDownTime, EntProtoId antagsToSpawn, bool cantRecall, bool endIfUnderPercent = true)
    {
        if ((float)_target.GetAliveHumans().Count / aliveOnSpawn >= percent)
            _ticker.StartGameRule(antagsToSpawn);
        else if(endIfUnderPercent)
            _roundEndSystem.RequestRoundEnd(countdownTime: countDownTime, cantRecall: cantRecall);
    }
}
