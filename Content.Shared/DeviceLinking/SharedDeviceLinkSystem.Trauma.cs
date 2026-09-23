namespace Content.Shared.DeviceLinking;

public abstract partial class SharedDeviceLinkSystem : EntitySystem
{
    /// <summary>
    /// Helper function that invokes a port with a high/low binary logic signal.
    /// </summary>
    public void SendSignal(Entity<DeviceLinkSourceComponent?> ent, string port, bool signal)
    {
        if (!DeviceLinkSourceQuery.Resolve(ent, ref ent.Comp))
            return;

        var data = new LogicStatePayload
        {
            State = signal ? SignalState.High : SignalState.Low
        };
        InvokePort(ent, port, ref data);

        ent.Comp.LastSignals[port] = signal;
    }
}
