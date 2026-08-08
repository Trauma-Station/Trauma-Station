// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.GameTicking;
using Content.Server.RoundEnd;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Trauma.Common.GameTicking;
using Content.Trauma.Server.GameTicking.Rules.Components;

namespace Content.Trauma.Server.GameTicking.Systems;

public sealed partial class NewAntagOrEvacSystem : CommonNewAntagOrEvacSystem
{
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private RoundEndSystem _roundEndSystem = default!;

    public override void SpawnNewAntagIfBelowPercent(EntityUid uid, TimeSpan countDownTime, bool cantRecall, bool endIfUnderPercent = true)
    {
        if (!TryComp<NewAntagOrEvacComponent>(uid, out var comp))
        {
            Log.Error($"Tried to SpawnNewAntagIfBelowPercent on entity: {ToPrettyString(uid)} but it didn't have NewAntagOrEvacComponent");
            return;
        }

        if ((float)_mind.GetAliveHumans().Count / comp.PlayersOnStart >= comp.Percent)
            _ticker.StartGameRule(comp.Event);
        else if (endIfUnderPercent)
            _roundEndSystem.RequestRoundEnd(countdownTime: countDownTime, cantRecall: cantRecall);
    }

    [SubscribeLocalEvent]
    private void OnGameRuleStarted(Entity<NewAntagOrEvacComponent> ent, ref GameRuleStartedEvent args)
    {
        ent.Comp.PlayersOnStart = _mind.GetAliveHumans().Count;
    }
}
