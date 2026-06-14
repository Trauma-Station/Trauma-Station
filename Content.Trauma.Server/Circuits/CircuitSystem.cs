// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;
using Content.Trauma.Shared.Circuits;

namespace Content.Trauma.Server.Circuits;

/// <summary>
/// Updates pulses for active circuits and handles their signals.
/// </summary>
public sealed partial class CircuitSystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _device = default!;
    [Dependency] private EntityQuery<CircuitComponent> _query = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CircuitHousingComponent, SignalReceivedEvent>(OnSignalReceived);

        SubscribeLocalEvent<CircuitComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<ActiveCircuitComponent, ComponentInit>(OnActiveInit);
        SubscribeLocalEvent<ActiveCircuitComponent, ComponentShutdown>(OnActiveShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveCircuitComponent, CircuitComponent>();
        while (query.MoveNext(out var uid, out _, out var comp))
        {
            var changed = comp.Changed;
            if (changed.Count == 0)
                return;

            comp.Changed = new();
            var gates = comp.Data.Gates;
            foreach (var i in changed)
            {
                if (!gates.TryGetValue(i, out var gate))
                    continue; // invalid...

                var old = gate.Output;
                gate.Update(comp);
                if (gate.Output.Equals(old))
                    continue; // no change

                foreach (var output in gate.LinkedOutputs)
                {
                    ValueChanged(comp, output, gate.Output);
                }
            }

            // change any momentary pulses back to low since theyve been processed
            for (var i = 0; i < comp.Inputs.Count; i++)
            {
                if (comp.Inputs[i] is not Pulse p)
                    continue;

                comp.Inputs[i] = False.Instance;
                foreach (var input in comp.LinkedInputs[i])
                {
                    ValueChanged(comp, input, False.Instance);
                }
            }
        }
    }

    private void OnSignalReceived(Entity<CircuitHousingComponent> ent, ref SignalReceivedEvent args)
    {
        if (!ent.Comp.Powered ||
            ent.Comp.Circuit is not { } circuit ||
            !args.Port.StartsWith("Circuit") || // ignore non circuit ports
            !_query.TryComp(circuit, out var comp))
            return;

        // holy goida
        var c = args.Port.Substring(7);
        if (!int.TryParse(c, out var i))
            return; // ignore non circuit ports, they end with a number

        i--; // the ids start with 1, convert to 0-based index
        // legacy signals with no data are assumed to be a pulse
        var value = args.Data is { } data ? ParseValue(data) : Pulse.Instance;
        if (comp.Inputs[i].Equals(value))
            return; // no change

        // process dependent gates next tick
        comp.Inputs[i] = value;
        foreach (var input in comp.LinkedInputs[i])
        {
            ValueChanged(comp, input, value);
        }
    }

    private void OnMapInit(Entity<CircuitComponent> ent, ref MapInitEvent args)
    {
        var data = ent.Comp.Data;
        ent.Comp.ValidatePortsCount();

        ent.Comp.LinkGateOutputs();

        // want to automatically update gates for premade circuits so you dont have to toggle inputs or whatever
        for (var i = 0; i < ent.Comp.LinkedInputs.Count; i++)
        {
            var list = ent.Comp.LinkedInputs[i];
            foreach (var linked in list)
            {
                if (linked.GateIndex is { } g)
                    ent.Comp.Changed.Add(g);
                else if (linked.PortIndex is { } p)
                    ent.Comp.LastOutputs[p] = ent.Comp.Inputs[i];
            }
        }
    }

    private void OnActiveInit(Entity<ActiveCircuitComponent> ent, ref ComponentInit args)
    {
        if (!_query.TryComp(ent, out var comp))
            return;

        // send expected values when a circuit is repowered installed etc
        for (var i = 0; i < comp.LastOutputs.Count; i++)
        {
            SendOutput(comp.Housing, i + 1, comp.LastOutputs[i]);
        }
    }

    private void OnActiveShutdown(Entity<ActiveCircuitComponent> ent, ref ComponentShutdown args)
    {
        if (!_query.TryComp(ent, out var comp))
            return;

        // stop sending values when a circuit is depowered removed etc
        for (var i = 0; i < CircuitComponent.PortsCount; i++)
        {
            if (!comp.LastOutputs[i].Equals(False.Instance))
                SendOutput(comp.Housing, i + 1, False.Instance);
        }
    }

    private object ParseValue(NetworkPayload data)
    {
        if (data.TryGetValue<SignalState>(DeviceNetworkConstants.LogicState, out var state))
            return state switch
            {
                SignalState.Momentary => Pulse.Instance,
                SignalState.High => True.Instance,
                _ => False.Instance
            };

        if (data.TryGetValue<int>("logic_int", out var n))
            return new Integer(n);

        if (data.TryGetValue<string>("logic_string", out var s))
            return s;

        return Pulse.Instance; // non-logic signals are assumed to be a pulse
    }

    private void ValueChanged(CircuitComponent comp, CircuitIndex idx, object value)
    {
        if (!comp.Data.ValidIndex(idx))
            return;

        if (idx.GateIndex is { } g)
            comp.Changed.Add(g); // update it next tick
        else if (idx.PortIndex is { } p)
            SendOutput(comp.Housing, p, comp.LastOutputs[p] = value); // send signal now
    }

    private void SendOutput(EntityUid? housing, int i, object value)
    {
        if (housing == null)
            return;

        var port = $"Circuit{i + 1}";

        // send new output signal to linked machines
        var payload = new NetworkPayload();
        switch (value)
        {
            case True t:
                payload[DeviceNetworkConstants.LogicState] = SignalState.High;
                break;
            case False f:
                payload[DeviceNetworkConstants.LogicState] = SignalState.Low;
                break;
            case Pulse p:
                payload[DeviceNetworkConstants.LogicState] = SignalState.Momentary;
                break;
            case Integer n:
                payload["logic_int"] = n.Value;
                break;
            case string s:
                payload["logic_string"] = s;
                break;
            default:
                Log.Error($"Tried to send unknown output {value} to port {port} of {ToPrettyString(housing)}!");
                return;
        }
        _device.InvokePort(housing.Value, port, payload);
    }
}
