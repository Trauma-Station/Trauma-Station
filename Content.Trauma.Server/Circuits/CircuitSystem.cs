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

    private NetworkPayload _payload = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CircuitHousingComponent, SignalReceivedEvent>(OnSignalReceived);

        SubscribeLocalEvent<CircuitComponent, ComponentInit>(OnInit);
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
                if (i < 0 || i >= gates.Count)
                    continue; // invalid

                var gate = gates[i];
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
            for (int i = 0; i < comp.Inputs.Count; i++)
            {
                if (comp.Inputs[i] is not Pulse p)
                    continue;

                comp.Inputs[i] = false;
                foreach (var input in comp.LinkedInputs[i])
                {
                    ValueChanged(comp, input, false);
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

        i -= 1; // the ids start with 1
        // legacy signals with no data are assumed to be a pulse
        var value = args.Data is { } data ? ParseValue(data) : Pulse.Instance;
        comp.Inputs[i] = value;
        foreach (var input in comp.LinkedInputs[i])
        {
            ValueChanged(comp, input, value);
        }
    }

    private void OnInit(Entity<CircuitComponent> ent, ref ComponentInit args)
    {
        // ensure required input port data exists
        while (ent.Comp.Inputs.Count < CircuitComponent.PortsCount)
            ent.Comp.Inputs.Add(false);
        while (ent.Comp.LinkedInputs.Count < CircuitComponent.PortsCount)
            ent.Comp.LinkedInputs.Add(new());
        while (ent.Comp.LastOutputs.Count < CircuitComponent.PortsCount)
            ent.Comp.LastOutputs.Add(false);

        var data = ent.Comp.Data;
        var gates = data.Gates;
        for (var i = 0; i < gates.Count; i++)
        {
            var gate = gates[i];
            foreach (var input in gate.Inputs)
            {
                if (input > 0 && input <= gates.Count)
                    gates[input - 1].LinkOutput(i + 1);
                else if (input < 0 && -input <= ent.Comp.LinkedInputs.Count)
                    ent.Comp.LinkInput(-input - 1, i + 1);
            }
        }

        for (var i = 0; i < data.OutputIndices.Count; i++)
        {
            var input = data.OutputIndices[i];
            if (input > 0 && input <= gates.Count)
                gates[input - 1].LinkOutput(-i - 1);
            else if (input < 0 && -input <= ent.Comp.LinkedInputs.Count)
                ent.Comp.LinkInput(-input - 1, -i - 1);
        }
    }

    private void OnMapInit(Entity<CircuitComponent> ent, ref MapInitEvent args)
    {
        // want to automatically update gates for premade circuits so you dont have to toggle inputs or whatever
        for (var i = 0; i < ent.Comp.LinkedInputs.Count; i++)
        {
            var list = ent.Comp.LinkedInputs[i];
            var value = ent.Comp.Inputs[i];
            foreach (var linked in list)
            {
                if (linked > 0) // ignore directly wired output ports since theres probably no housing
                    ent.Comp.Changed.Add(linked - 1);
                else if (linked < 0 && -linked <= ent.Comp.LastOutputs.Count)
                    ent.Comp.LastOutputs[-linked - 1] = value;
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
            if (!comp.LastOutputs[i].Equals(false))
                SendOutput(comp.Housing, i + 1, false);
        }
    }

    private object ParseValue(NetworkPayload data)
    {
        if (data.TryGetValue<SignalState>(DeviceNetworkConstants.LogicState, out var state))
        {
            return state switch
            {
                SignalState.Momentary => Pulse.Instance,
                SignalState.Low => false,
                SignalState.High => true,
                _ => false
            };
        }

        if (data.TryGetValue<int>("logic_int", out var n))
            return n;

        if (data.TryGetValue<string>("logic_string", out var s))
            return s;

        return false; // its a mystery
    }

    private void ValueChanged(CircuitComponent comp, int i, object value)
    {
        if (i > 0)
            comp.Changed.Add(i - 1); // update it next tick
        else if (i < 0 && -i <= CircuitComponent.PortsCount)
            SendOutput(comp.Housing, -i, comp.LastOutputs[-i - 1] = value); // send signal now
    }

    private void SendOutput(EntityUid housing, int i, object value)
    {
        var port = $"Circuit{i}";

        // send new output signal to linked machines
        _payload.Clear();
        switch (value)
        {
            case bool b:
                _payload[DeviceNetworkConstants.LogicState] = b ? SignalState.High : SignalState.Low;
                break;
            case Pulse p: // should probably never happen but just incase
                _payload[DeviceNetworkConstants.LogicState] = SignalState.Momentary;
                break;
            case int n:
                _payload["logic_int"] = n;
                break;
            case string s:
                _payload["logic_string"] = s;
                break;
            default:
                Log.Error($"Tried to send unknown output {value} to port {port} of {ToPrettyString(housing)}!");
                return;
        }
        _device.InvokePort(housing, port, _payload);
    }
}
