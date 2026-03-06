// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chat.Systems;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Systems;
using Content.Shared._White.Xenomorphs.Queen;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
// </Trauma>

namespace Content.Server._White.Xenomorphs;

// <Trauma>
public sealed class XenomorphQueenShuttleRecallSystem : EntitySystem
{
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergency = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private TimeSpan _nextCheck = TimeSpan.Zero;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextCheck)
            return;

        _nextCheck = _timing.CurTime + CheckInterval;

        var queenAlive = false;

        // Get the station map so we can check if the queen is physically on it.
        var stationMap = _roundEnd.GetStation();

        if (stationMap != null)
        {
            var queenQuery = EntityQueryEnumerator<XenomorphQueenComponent, MobStateComponent, TransformComponent>();
            while (queenQuery.MoveNext(out _, out _, out var mobState, out var xform))
            {
                if (mobState.CurrentState == MobState.Dead)
                    continue;

                // Only count queens that are on the same map as the station.
                if (xform.MapUid != stationMap)
                    continue;

                queenAlive = true;
                break;
            }
        }

        // If the shuttle was already called and the queen is alive, force recall it.
        if (queenAlive && _roundEnd.ExpectedCountdownEnd != null && !_emergency.EmergencyShuttleArrived)
        {
            _roundEnd.CancelRoundEndCountdown(forceRecall: true);
            _chat.DispatchGlobalAnnouncement(
                Loc.GetString("xeno-queen-shuttle-recall-announcement"),
                Loc.GetString("comms-console-announcement-title-centcom"),
                colorOverride: Color.Red);
        }
    }
}
// </Trauma>
