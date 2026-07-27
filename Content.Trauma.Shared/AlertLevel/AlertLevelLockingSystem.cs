// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.AlertLevel;
using Content.Trauma.Common.AlertLevel;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.AlertLevel;

/// <summary>
/// Prevents a station going to set alert levels without being on a required alert level for some time beforehand.
/// </summary>
public sealed partial class AlertLevelLockingSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnChangeAlertLevelAttempt(Entity<AlertLevelLockingComponent> ent, ref ChangeAlertLevelAttemptEvent args)
    {
        // don't care about non-locked alert
        if (args.AlertLevel != ent.Comp.LockedLevel || args.AlertLevel == args.CurrentLevel)
            return;

        // allow it if on the required alert level for enough time
        if (ent.Comp.NextUnlock is {} unlock && _timing.CurTime >= unlock)
            return;

        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnCheckAlertLevelLock(Entity<AlertLevelLockingComponent> ent, ref CheckAlertLevelLockEvent args)
    {
        args.LockedLevel = ent.Comp.LockedLevel;
        args.NextUnlock = ent.Comp.NextUnlock;
    }

    [SubscribeLocalEvent]
    private void OnAlertLevelChanged(Entity<AlertLevelLockingComponent> ent, ref AlertLevelChangedEvent args)
    {
        ent.Comp.NextUnlock = args.AlertLevel == ent.Comp.RequiredLevel
            // switched to the required alert, start the timer
            ? _timing.CurTime + ent.Comp.LockTime
            // when switching to a non-required alert reset the timer
            : null;
        Dirty(ent);
    }
}
