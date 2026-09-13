// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Power.EntitySystems;

namespace Content.Goobstation.Shared.Factory;

public sealed partial class StartableMachineSystem : EntitySystem
{
    [Dependency] private SharedDeviceLinkSystem _device = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private EntityQuery<StartableMachineComponent> _query = default!;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<StartableMachineComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.AutoStartQueued)
                continue;

            comp.AutoStartQueued = false;
            TryAutoStart((uid, comp));
        }
    }

    [SubscribeLocalEvent]
    private void OnInit(Entity<StartableMachineComponent> ent, ref ComponentInit args)
    {
        _device.EnsureSinkPorts(ent, ent.Comp.StartPort, ent.Comp.AutoStartPort);
        _device.EnsureSourcePorts(ent, ent.Comp.StartedPort, ent.Comp.CompletedPort, ent.Comp.FailedPort);
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<StartableMachineComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port == ent.Comp.StartPort)
        {
            TryStart((ent, ent.Comp));
        }
        else if (args.Port == ent.Comp.AutoStartPort)
        {
            ent.Comp.AutoStart = !ent.Comp.AutoStart;
        }
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<StartableMachineComponent> ent, ref SignalReceivedEvent<LogicStatePayload> args)
    {
        if (args.Port == ent.Comp.StartPort && args.Data.State != SignalState.Low)
        {
            TryStart((ent, ent.Comp));
        }
        else if (args.Port == ent.Comp.AutoStartPort)
        {
            ent.Comp.AutoStart = args.Data.State switch
            {
                SignalState.Momentary => !ent.Comp.AutoStart,
                SignalState.High => true,
                _ => false
            };
        }
    }

    #region Public API

    /// <summary>
    /// Starts the machine if powered.
    /// </summary>
    public bool TryStart(Entity<StartableMachineComponent?> ent)
    {
        if (!_query.Resolve(ent, ref ent.Comp)
            || !_power.IsPowered(ent.Owner))
            return false;

        var ev = new MachineStartedEvent();
        RaiseLocalEvent(ent, ref ev);
        return true;
    }

    /// <summary>
    /// Starts the machine if powered and autostart is enabled.
    /// </summary>
    public bool TryAutoStart(Entity<StartableMachineComponent?> ent)
    {
        if (!_query.Resolve(ent, ref ent.Comp)
            || !ent.Comp.AutoStart)
            return false;

        return TryStart(ent);
    }

    /// <summary>
    /// Invokes a port if the machine is powered.
    /// </summary>
    public void InvokeIfPowered(EntityUid uid, [ForbidLiteral] string port)
    {
        if (_power.IsPowered(uid))
            _device.InvokePort(uid, port);
    }

    /// <summary>
    /// Invoke the start port if powered.
    /// </summary>
    public void Started(Entity<StartableMachineComponent?> ent)
    {
        if (!_query.Resolve(ent, ref ent.Comp))
            return;

        InvokeIfPowered(ent, ent.Comp.StartedPort);
    }

    /// <summary>
    /// Invoke the completed port if powered.
    /// Also queues an autostart if <c>autoStart</c> is true
    /// </summary>
    public void Completed(Entity<StartableMachineComponent?> ent, bool autoStart = true)
    {
        if (!_query.Resolve(ent, ref ent.Comp))
            return;

        InvokeIfPowered(ent, ent.Comp.CompletedPort);

        if (autoStart)
            ent.Comp.AutoStartQueued = true;
    }

    /// <summary>
    /// Invoke the failed port if powered.
    /// </summary>
    public void Failed(Entity<StartableMachineComponent?> ent)
    {
        if (!_query.Resolve(ent, ref ent.Comp))
            return;

        InvokeIfPowered(ent, ent.Comp.FailedPort);
    }

    #endregion
}
