

using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Lock;
using JetBrains.Annotations;

namespace Content.Trauma.Server.DeviceLinking;

public sealed partial class LockSignalControlSystem : EntitySystem
{
    [Dependency] private LockSystem _lockSystem = default!;
    [Dependency] private DeviceLinkSystem _signalSystem = default!;

    [SubscribeLocalEvent]
    private void OnInit(Entity<LockSignalControlComponent> ent, ref ComponentInit args)
    {
        _signalSystem.EnsureSinkPorts(ent, ent.Comp.LockPort, ent.Comp.UnlockPort, ent.Comp.ToggleLockPort);
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<LockSignalControlComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port == ent.Comp.LockPort)
            _lockSystem.Lock(ent, args.Trigger);
        else if (args.Port == ent.Comp.UnlockPort)
            _lockSystem.Unlock(ent, args.Trigger);
        else if (args.Port == ent.Comp.ToggleLockPort)
            _lockSystem.ToggleLock(ent, args.Trigger);
    }
}