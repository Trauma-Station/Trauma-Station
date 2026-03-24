using Content.Shared.Power.Components;

namespace Content.Trauma.Shared.ClockworkCult.Scripture;

/// <summary>
/// This handles battery related events for reciting scriptures
/// </summary>
public sealed partial class ScriptureSystem
{
    public void InitializeBattery()
    {
        SubscribeLocalEvent<BatteryComponent, ReciteAttemptEvent>(OnRecite);
    }

    private void OnRecite(Entity<BatteryComponent> ent, ref ReciteAttemptEvent args)
    {
        if (_battery.GetCharge(ent.AsNullable()) >= args.ScriptureCost)
            return;

        Log.Debug("Not Enough Power!");
        args.Cancelled = true;
    }
}
