

using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Lock;

namespace Content.Trauma.Shared.DeviceLinking;

[Access(typeof(LockSignalControlSystem))]
public sealed partial class LockSignalControlSystem : EntitySystem
{
    [Dependency] private LockSystem _lock = default!;
    [Dependency] private SharedDeviceLinkSystem _signal = default!;

    [SubscribeLocalEvent]
    private void OnInit(Entity<LockSignalControlComponent> ent, ref ComponentInit args)
    {
        _signal.EnsureSinkPorts(ent, ent.Comp.LockPort, ent.Comp.UnlockPort, ent.Comp.ToggleLockPort);
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<LockSignalControlComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port == ent.Comp.LockPort)
            _lock.Lock(ent, args.Trigger);
        else if (args.Port == ent.Comp.UnlockPort)
            _lock.Unlock(ent, args.Trigger);
        else if (args.Port == ent.Comp.ToggleLockPort)
            _lock.ToggleLock(ent, args.Trigger);
    }
}
