using Content.Server.AlertLevel;
using Content.Trauma.Shared.AlertLevel;

namespace Content.Trauma.Server.AlertLevel;

public sealed class AlertLevelLockingSystem : SharedAlertLevelLockingSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlertLevelLockingComponent, AlertLevelChangedEvent>(OnAlertLevelChanged);
    }

    private void OnAlertLevelChanged(Entity<AlertLevelLockingComponent> ent, ref AlertLevelChangedEvent args)
    {
        Dirty(ent);
        if (args.AlertLevel != ent.Comp.RequiredLevel)
        {
            // when switching to a non-required alert reset the timer
            ent.Comp.NextUnlock = null;
            return;
        }

        // switched to the required alert, start the timer
        ent.Comp.NextUnlock = Timing.CurTime + ent.Comp.LockTime;
    }
}
