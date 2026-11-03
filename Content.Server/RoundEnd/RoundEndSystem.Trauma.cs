using System.Threading;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.RoundEnd;

/// <summary>
/// Trauma - blood cult shitcode
/// </summary>
public sealed partial class RoundEndSystem
{
    /// <summary>
    /// If evac is called, delays it by some time.
    /// Does nothing if it wasn't already called.
    /// </summary>
    public void DelayShuttle(TimeSpan delay)
    {
        if (_countdownTokenSource == null || ExpectedCountdownEnd is not {} end)
            return;

        var countdown = end - _gameTiming.CurTime + delay;
        if (countdown.TotalSeconds < 0)
            return;

        ExpectedCountdownEnd = _gameTiming.CurTime + countdown;
        _countdownTokenSource.Cancel();
        _countdownTokenSource = new CancellationTokenSource();

        // TODO: if upstream ever refactors round end kill this slop
        Timer.Spawn(countdown, () => RequestRoundEnd(), _countdownTokenSource.Token);
    }
}
