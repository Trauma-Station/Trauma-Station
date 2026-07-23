using Content.Server.GameTicking;
using Content.Server.RoundEnd;
using Content.Shared.Objectives.Systems;
using Content.Trauma.Common.GameTicking.Events;

namespace Content.Trauma.Server.GameTicking.Systems;

/// <summary>
/// Helper method for deciding if a new antag is required
/// </summary>
public sealed partial class NewAntagDeciderSystem : EntitySystem
{
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private TargetSystem _target = default!;
    [Dependency] private RoundEndSystem _roundEndSystem = default!;

    [SubscribeLocalEvent]
    public void SpawnNewAntagIfBelowPercent(ref RequestNewAntagOrCallEvacEvent args)
    {
        if ((float)_target.GetAliveHumans().Count / args.AliveOnSpawn >= args.Percent)
            _ticker.StartGameRule(args.AntagsToSpawn);
        else if(args.EndIfUnderPercent)
            _roundEndSystem.RequestRoundEnd(countdownTime: args.CountDownTime, cantRecall: args.CantRecall);
    }
}
