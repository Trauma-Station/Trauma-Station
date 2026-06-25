// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Nuclear;

public abstract partial class SharedNuclearMachineSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected EntityQuery<NuclearMachineComponent> Query = default!;

    private TimeSpan _logDelay = TimeSpan.FromSeconds(1.5); // admin logs arent made unless unchanged for this long, to avoid spamming

    protected void SendLog(Entity<NuclearMachineComponent> ent)
    {
        ent.Comp.NextLog = null;
        var user = ent.Comp.LastUser;
        if (!Exists(user))
            return;

        var monitor = ent.Comp.LastMonitor ?? ent.Owner;
        var ev = new NuclearMachineLogEvent(user, monitor);
        RaiseLocalEvent(ent, ref ev);
    }

    /// <summary>
    /// Queue a log change when a user modifies something.
    /// </summary>
    public void QueueLog(EntityUid uid, EntityUid user, EntityUid? monitor = null)
    {
        if (!Query.TryComp(uid, out var comp))
            return;

        comp.LastUser = user;
        comp.LastMonitor = monitor;
        comp.NextLog = Timing.CurTime + _logDelay;
    }
}
