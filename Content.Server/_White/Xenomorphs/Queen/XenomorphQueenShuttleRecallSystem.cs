// # Trauma

using Content.Server.Chat.Systems;
using Content.Server.RoundEnd;
using Content.Shared._White.Xenomorphs.Queen;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Localization;
using Robust.Shared.Log;

namespace Content.Server._White.Xenomorphs;

public sealed class XenomorphQueenShuttleRecallSystem : EntitySystem
{
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    private const float CheckInterval = 5f;
    private float _timer;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _timer += frameTime;
        if (_timer < CheckInterval)
            return;
        _timer = 0f;

        var queenAlive = false;

        var queenQuery = EntityQueryEnumerator<XenomorphQueenComponent, MobStateComponent>();
        while (queenQuery.MoveNext(out _, out _, out var mobState))
        {
            if (mobState.CurrentState != MobState.Dead)
            {
                queenAlive = true;
                break;
            }
        }

        // # Block the shuttle from being called while the queen is alive.
        // # When the queen dies, allow it again.
        _roundEnd.CantRecall = queenAlive;

        // # If the shuttle was already called and the queen is alive, force recall it — mirroring how the blob does it.
        if (queenAlive && _roundEnd.ExpectedCountdownEnd != null)
        {
            _roundEnd.CancelRoundEndCountdown(forceRecall: true);
            _chat.DispatchGlobalAnnouncement(
                Loc.GetString("xeno-queen-shuttle-recall-announcement"),
                Loc.GetString("xeno-queen-shuttle-recall-sender"),
                colorOverride: Color.Red);

            Log.Info("Xenomorph Queen is alive — emergency shuttle recalled.");
        }
    }
}
