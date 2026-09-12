// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;
using Content.Trauma.Common.DeviceLinking;
using Content.Trauma.Shared.Circuits;

namespace Content.Trauma.Server.Circuits;

/// <summary>
/// Updates pulses for active circuits and handles their signals.
/// </summary>
public sealed partial class CircuitSystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _device = default!;
    [Dependency] private EntityQuery<CircuitComponent> _query = default!;

    private List<int> _changed = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveCircuitComponent, CircuitComponent>();
        while (query.MoveNext(out _, out _, out var comp))
        {
            // update any momentary pulses's gates
            for (var i = 0; i < comp.Inputs.Count; i++)
            {
                if (comp.Inputs[i] != Pulse.Instance)
                    continue;

                foreach (var input in comp.LinkedInputs[i])
                {
                    ValueChanged(comp, input, False.Instance);
                }
            }

            UpdateChangedGates(comp);

            // change any momentary pulses back to low since theyve been processed
            for (var i = 0; i < comp.Inputs.Count; i++)
            {
                if (comp.Inputs[i] != Pulse.Instance)
                    continue;

                comp.Inputs[i] = False.Instance;
            }
        }
    }

    private void UpdateChangedGates(CircuitComponent comp)
    {
        if (comp.Changed.Count == 0)
            return;

        _changed.Clear();
        _changed.AddRange(comp.Changed);
        comp.Changed.Clear();
        var gates = comp.Data.Gates;
        foreach (var i in _changed)
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
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<CircuitHousingComponent> ent, ref SignalReceivedEvent args)
    {
        // legacy signals with no data are assumed to be a pulse
        TrySetInput(ent, args.Port, Pulse.Instance);
    }

    [SubscribeLocalEvent]
    private void OnSignalStateReceived(Entity<CircuitHousingComponent> ent, ref SignalReceivedEvent<LogicStatePayload> args)
    {
        TrySetInput(ent, args.Port, args.Data.State switch
        {
            SignalState.Momentary => Pulse.Instance,
            SignalState.High => True.Instance,
            _ => False.Instance
        });
    }

    [SubscribeLocalEvent]
    private void OnSignalIntReceived(Entity<CircuitHousingComponent> ent, ref SignalReceivedEvent<LogicIntPayload> args)
    {
        TrySetInput(ent, args.Port, new Integer(args.Data.Value));
    }

    [SubscribeLocalEvent]
    private void OnSignalStringReceived(Entity<CircuitHousingComponent> ent, ref SignalReceivedEvent<LogicStringPayload> args)
    {
        TrySetInput(ent, args.Port, args.Data.Value);
    }

    private void TrySetInput(Entity<CircuitHousingComponent> ent, string port, object value)
    {
        if (!ent.Comp.Powered ||
            ent.Comp.Circuit is not { } circuit ||
            !port.StartsWith("Circuit") || // ignore non circuit ports
            !_query.TryComp(circuit, out var comp))
            return;

        // holy goida
        var c = port.Substring(7);
        if (!int.TryParse(c, out var i))
            return; // ignore non circuit ports, they end with a number

        i--; // the ids start with 1, convert to 0-based index
        if (comp.Inputs[i].Equals(value))
            return; // no change

        // process dependent gates next tick
        comp.Inputs[i] = value;
        foreach (var input in comp.LinkedInputs[i])
        {
            ValueChanged(comp, input, value);
        }
    }

    [SubscribeLocalEvent]
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
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnActiveInit(Entity<ActiveCircuitComponent> ent, ref ComponentInit args)
    {
        if (!_query.TryComp(ent, out var comp))
            return;

        // send expected values when a circuit is repowered installed etc
        var gates = comp.Data.Gates;
        for (var o = 0; o < comp.Data.OutputIndices.Count; o++)
        {
            var i = comp.Data.OutputIndices[o];
            if (i.GateIndex is { } g)
                SendOutput(comp.Housing, o, gates[g].Output);
            else if (i.PortIndex is { } p)
                SendOutput(comp.Housing, o, comp.Inputs[p]);
        }
    }

    [SubscribeLocalEvent]
    private void OnActiveShutdown(Entity<ActiveCircuitComponent> ent, ref ComponentShutdown args)
    {
        if (!_query.TryComp(ent, out var comp))
            return;

        // stop sending values when a circuit is depowered removed etc
        for (var i = 0; i < CircuitComponent.PortsCount; i++)
        {
            SendOutput(comp.Housing, i, False.Instance);
        }
    }

    private void ValueChanged(CircuitComponent comp, CircuitIndex idx, object value)
    {
        if (!comp.Data.ValidIndex(idx))
            return;

        if (idx.GateIndex is { } g)
            comp.Changed.Add(g); // update it next tick
        else if (idx.PortIndex is { } p)
            SendOutput(comp.Housing, p, value); // send signal now
    }

    private void SendOutput(EntityUid? housing, int i, object value)
    {
        if (housing == null)
            return;

        var port = $"Circuit{i + 1}";

        // send new output signal to linked machines
        switch (value)
        {
            case True t:
                var truePayload = new LogicStatePayload { State = SignalState.High };
                _device.InvokePort(housing.Value, port, ref truePayload);
                break;
            case False f:
                var falsePayload = new LogicStatePayload { State = SignalState.Low };
                _device.InvokePort(housing.Value, port, ref falsePayload);
                break;
            case Pulse p:
                var pulsePayload = new LogicStatePayload { State = SignalState.Momentary };
                _device.InvokePort(housing.Value, port, ref pulsePayload);
                break;
            case Integer n:
                var intPayload = new LogicIntPayload(n.Value);
                _device.InvokePort(housing.Value, port, ref intPayload);
                break;
            case string s:
                var stringPayload = new LogicStringPayload(s);
                _device.InvokePort(housing.Value, port, ref stringPayload);
                break;
            default:
                Log.Error($"Tried to send unknown output {value} to port {port} of {ToPrettyString(housing)}!");
                return;
        }
    }
}
