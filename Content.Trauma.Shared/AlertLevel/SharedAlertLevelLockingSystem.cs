using Content.Trauma.Common.AlertLevel;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.AlertLevel;

/// <summary>
/// Prevents a station going to certain alert levels without being on a required alert level for some time beforehand.
/// </summary>
public abstract class SharedAlertLevelLockingSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlertLevelLockingComponent, ChangeAlertLevelAttemptEvent>(OnChangeAlertLevelAttempt);
        SubscribeLocalEvent<AlertLevelLockingComponent, CheckAlertLevelLockEvent>(OnCheckAlertLevelLock);
    }

    private void OnChangeAlertLevelAttempt(Entity<AlertLevelLockingComponent> ent, ref ChangeAlertLevelAttemptEvent args)
    {
        // don't care about non-locked alert
        if (args.AlertLevel != ent.Comp.LockedLevel)
            return;

        // allow it if on the required alert level for enough time
        if (ent.Comp.NextUnlock is {} unlock && Timing.CurTime >= unlock)
            return;

        args.Cancel();
    }

    private void OnCheckAlertLevelLock(Entity<AlertLevelLockingComponent> ent, ref CheckAlertLevelLockEvent args)
    {
        args.LockedLevel = ent.Comp.LockedLevel;
        args.NextUnlock = ent.Comp.NextUnlock;
    }
}
