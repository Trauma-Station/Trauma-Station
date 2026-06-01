// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Administration.Logs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Circuits;

public sealed partial class CircuitEditorSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private EntityQuery<CircuitComponent> _query = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CircuitEditorComponent, BeforeActivatableUIOpenEvent>(OnUIOpen);
        SubscribeLocalEvent<CircuitEditorComponent, EntInsertedIntoContainerMessage>(OnCircuitChanged);
        SubscribeLocalEvent<CircuitEditorComponent, EntRemovedFromContainerMessage>(OnCircuitChanged);
        Subs.BuiEvents<CircuitEditorComponent>(CircuitEditorUiKey.Key, subs =>
        {
            subs.Event<CircuitEditorClearMessage>(OnClear);
            subs.Event<CircuitEditorImportMessage>(OnImport);
            subs.Event<CircuitEditorAddGateMessage>(OnAddGate);
            subs.Event<CircuitEditorMoveGateMessage>(OnMoveGate);
            subs.Event<CircuitEditorRemoveGateMessage>(OnRemoveGate);
            subs.Event<CircuitEditorLinkMessage>(OnLink);
            subs.Event<CircuitEditorUnlinkMessage>(OnUnlink);
        });
    }

    private void OnUIOpen(Entity<CircuitEditorComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateUI(ent);
    }

    private void OnCircuitChanged(EntityUid uid, CircuitEditorComponent comp, ContainerModifiedMessage args)
    {
        if (args.Container.ID == comp.SlotId)
            UpdateUI((uid, comp));
    }

    private void OnClear(Entity<CircuitEditorComponent> ent, ref CircuitEditorClearMessage args)
    {
        if (GetCircuit(ent) is not { } circuit)
            return;

        var size = circuit.Comp.Data.Gates.Count;
        if (size == 0)
            return; // already empty

        ClearCircuit(circuit);

        _adminLog.Add(LogType.Circuits, LogImpact.Medium, $"Circuit {circuit.Owner} with {size} gates cleared by {args.Actor} using {ent.Owner}");
        UpdateUI(ent);
    }

    private void OnImport(Entity<CircuitEditorComponent> ent, ref CircuitEditorImportMessage args)
    {
        if (GetCircuit(ent) is not { } circuit)
            return;

        var data = args.Data;
        if (data.Gates.Count > CircuitComponent.MaxGates)
        {
            _popup.PopupPredictedCursor("Circuit has too many gates to import!", args.Actor, PopupType.MediumCaution);
            return;
        }

        for (var i = 0; i < data.Gates.Count; i++)
        {
            var gate = data.Gates[i];
            gate.Initialize();
            gate.Validate();
            circuit.Comp.Changed.Add(i); // fully recompute it when powered to account for clocks etc
        }

        circuit.Comp.Data = data;
        circuit.Comp.ValidatePortsCount();
        circuit.Comp.LinkGateOutputs();

        var size = data.Gates.Count;
        _adminLog.Add(LogType.Circuits, LogImpact.Medium, $"Circuit {circuit.Owner} imported {size} gates by {args.Actor} using {ent.Owner}");
        UpdateUI(ent);
    }

    private void OnAddGate(Entity<CircuitEditorComponent> ent, ref CircuitEditorAddGateMessage args)
    {
        if (GetCircuit(ent) is not { } circuit)
            return;

        var data = circuit.Comp.Data;
        var i = data.Gates.Count;
        if (i >= CircuitComponent.MaxGates)
        {
            _popup.PopupPredictedCursor("Circuit is full!", args.Actor, PopupType.MediumCaution);
            return;
        }

        var gate = args.Gate;
        gate.Validate();
        gate.Initialize();
        data.Gates.Add(gate);
        circuit.Comp.Changed.Add(i); // update the gate when its next powered
        UpdateUI(ent);
    }

    private void OnMoveGate(Entity<CircuitEditorComponent> ent, ref CircuitEditorMoveGateMessage args)
    {
        if (args.Index < 0 || GetCircuit(ent) is not { } circuit)
            return;

        var gates = circuit.Comp.Data.Gates;
        if (args.Index >= gates.Count)
            return;

        var gate = gates[args.Index];
        gate.Pos = args.Pos;
        gate.Validate();
        UpdateUI(ent);
    }

    private void OnRemoveGate(Entity<CircuitEditorComponent> ent, ref CircuitEditorRemoveGateMessage args)
    {
        if (!_timing.IsFirstTimePredicted || args.Index < 0 || GetCircuit(ent) is not { } circuit)
            return;

        var data = circuit.Comp.Data;
        var gates = data.Gates;
        if (args.Index >= gates.Count)
            return;

        var output = CircuitIndex.Gate(args.Index);
        var gate = gates.RemoveSwap(args.Index);

        // have to remove all references to the removed gate
        foreach (var index in gate.Inputs)
        {
            circuit.Comp.UnlinkOutput(index, output);
        }
        foreach (var index in gate.LinkedOutputs)
        {
            if (!data.ValidIndex(index))
                continue;

            if (index.GateIndex is { } g)
                SwapValue(gates[g].Inputs, output, CircuitIndex.Invalid); // unlink from a gate
            else if (index.PortIndex is { } p && p < data.OutputIndices.Count)
                data.OutputIndices[p] = CircuitIndex.Invalid; // unlink from a output port of the circuit
        }

        if (!gates.TryGetValue(args.Index, out gate))
        {
            // no gate left to replace it, done now
            UpdateUI(ent);
            return;
        }

        // and the gate that replaced it
        var oldIndex = CircuitIndex.Gate(gates.Count);
        foreach (var index in gate.Inputs)
        {
            if (!data.ValidIndex(index))
                continue;

            if (index.GateIndex is { } g)
                SwapValue(gates[g].LinkedOutputs, oldIndex, output);
            else if (index.PortIndex is { } p && circuit.Comp.LinkedInputs.TryGetValue(p, out var inputs))
                SwapValue(inputs, oldIndex, output);
        }
        foreach (var index in gate.LinkedOutputs)
        {
            if (!data.ValidIndex(index))
                continue;

            if (index.GateIndex is { } g)
                SwapValue(gates[g].Inputs, oldIndex, output);
            else if (index.PortIndex is { } p && p < data.OutputIndices.Count)
                data.OutputIndices[p] = output;
        }

        UpdateUI(ent);
    }

    private void OnLink(Entity<CircuitEditorComponent> ent, ref CircuitEditorLinkMessage args)
    {
        if (GetCircuit(ent) is not { } circuit)
            return;

        var data = circuit.Comp.Data;
        var gates = data.Gates;
        var output = args.Index;
        var input = args.Input;
        if (!data.ValidIndex(input))
            return; // bounds check this upfront so it doesnt get half set

        if (output.GateIndex is { } g)
        {
            var gate = gates[g];
            if (args.N >= gate.Inputs.Count)
                return;

            gate.Inputs[args.N] = input;
        }
        else if (output.PortIndex is { } p && p < data.OutputIndices.Count)
        {
            data.OutputIndices[p] = input;
        }
        else
        {
            return; // no linking something to invalid
        }

        circuit.Comp.LinkOutput(input, output);

        UpdateUI(ent);
    }

    private void OnUnlink(Entity<CircuitEditorComponent> ent, ref CircuitEditorUnlinkMessage args)
    {
        if (GetCircuit(ent) is not { } circuit)
            return;

        var data = circuit.Comp.Data;
        var gates = data.Gates;
        var output = args.Index;
        if (!data.ValidIndex(output))
            return;

        var old = CircuitIndex.Invalid;
        if (output.GateIndex is { } g)
        {
            var gate = gates[g];
            if (args.N >= gate.Inputs.Count)
                return;

            old = gate.Inputs[args.N];
            gate.Inputs[args.N] = CircuitIndex.Invalid;
        }
        else if (output.PortIndex is { } p)
        {
            old = data.OutputIndices[p];
            data.OutputIndices[p] = CircuitIndex.Invalid;
        }

        // clean up backreferences
        if (data.ValidIndex(old))
        {
            if (old.GateIndex is { } og)
                gates[og].LinkedOutputs.Remove(output);
            else if (old.PortIndex is { } op)
                circuit.Comp.LinkedInputs[op].Remove(output);
        }

        UpdateUI(ent);
    }

    public Entity<CircuitComponent>? GetCircuit(Entity<CircuitEditorComponent> ent)
        => _slots.GetItemOrNull(ent.Owner, ent.Comp.SlotId) is { } uid &&
            _query.TryComp(uid, out var comp)
            ? (uid, comp)
            : null;

    public void ClearCircuit(Entity<CircuitComponent> ent)
    {
        ent.Comp.Inputs.Clear();
        ent.Comp.LinkedInputs.Clear();
        ent.Comp.Changed.Clear(); // just incase...
        ent.Comp.Data = new();
        ent.Comp.ValidatePortsCount();
    }

    public void UpdateUI(Entity<CircuitEditorComponent> ent)
    {
        var circuit = GetCircuit(ent);
        var data = circuit?.Comp.Data;
        var state = new CircuitEditorState(data, GetNetEntity(circuit?.Owner));
        _ui.SetUiState(ent.Owner, CircuitEditorUiKey.Key, state);
    }

    private static void SwapValue(List<CircuitIndex> list, CircuitIndex from, CircuitIndex to)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == from)
                list[i] = to;
        }
    }
}
