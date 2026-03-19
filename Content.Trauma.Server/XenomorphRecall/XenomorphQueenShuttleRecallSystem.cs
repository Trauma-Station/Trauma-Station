// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chat.Systems;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Systems;
using Content.Shared._White.Xenomorphs.Queen;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Localization;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.XenomorphRecall;

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

        if (_roundEnd.ExpectedCountdownEnd == null || _emergency.EmergencyShuttleArrived)
            return;

        var queenAlive = false;

        if (_roundEnd.GetStation() is not {} stationMap)
            return;

        var queenQuery = EntityQueryEnumerator<XenomorphQueenComponent, MobStateComponent, TransformComponent>();
        while (queenQuery.MoveNext(out _, out _, out var mobState, out var xform))
        {
            if (mobState.CurrentState == MobState.Dead)
                continue;

            if (xform.MapUid != stationMap)
                continue;

            queenAlive = true;
            break;
        }

        if (queenAlive)
        {
            _roundEnd.CancelRoundEndCountdown(forceRecall: true);
            _chat.DispatchGlobalAnnouncement(
                Loc.GetString("xeno-queen-shuttle-recall-announcement"),
                Loc.GetString("comms-console-announcement-title-centcom"),
                colorOverride: Color.Red);
        }
    }
}
