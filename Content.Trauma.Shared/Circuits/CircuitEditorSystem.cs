// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Administration.Logs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Circuits;

public sealed partial class CircuitEditorSystem : EntitySystem
{
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

        foreach (var gate in data.Gates)
        {
            gate.Initialize();
            gate.Validate();
        }

        var size = data.Gates.Count;
        _adminLog.Add(LogType.Circuits, LogImpact.Medium, $"Circuit {circuit.Owner} imported {size} gates by {args.Actor} using {ent.Owner}");
        UpdateUI(ent);
    }

    private void OnAddGate(Entity<CircuitEditorComponent> ent, ref CircuitEditorAddGateMessage args)
    {
        if (GetCircuit(ent) is not { } circuit)
            return;

        var data = circuit.Comp.Data;
        if (data.Gates.Count >= CircuitComponent.MaxGates)
        {
            _popup.PopupPredictedCursor("Circuit is full!", args.Actor, PopupType.MediumCaution);
            return;
        }

        var gate = args.Gate;
        gate.Validate();
        gate.Initialize();
        data.Gates.Add(gate);
        UpdateUI(ent);
    }

    private void OnMoveGate(Entity<CircuitEditorComponent> ent, ref CircuitEditorMoveGateMessage args)
    {
        if (args.Index < 0 || GetCircuit(ent) is not { } circuit)
            return;

        var gates = circuit.Comp.Data.Gates;
        if (args.Index >= gates.Count)
            return;

        var gate = gates[args.Index - 1];
        gate.Pos = args.Pos;
        gate.Validate();
        UpdateUI(ent);
    }

    private void OnRemoveGate(Entity<CircuitEditorComponent> ent, ref CircuitEditorRemoveGateMessage args)
    {
        if (args.Index < 0 || GetCircuit(ent) is not { } circuit)
            return;

        var data = circuit.Comp.Data;
        var gates = data.Gates;
        if (args.Index >= gates.Count)
            return;

        var gate = gates.RemoveSwap(args.Index);
        if (gates.Count == 0)
        {
            ClearCircuit(circuit); // reset it when empty to make logic easier
            UpdateUI(ent);
            return;
        }

        // have to remove all references to the removed gate
        foreach (var index in gate.Inputs)
        {
            if (index > 0 && index <= gates.Count)
                gates[index - 1].LinkedOutputs.RemoveSwap(args.Index);
            else if (index < 0 && -index <= circuit.Comp.LinkedInputs.Count)
                circuit.Comp.LinkedInputs[-index - 1].RemoveSwap(index);
        }
        foreach (var index in gate.LinkedOutputs)
        {
            if (index > 0 && index <= gates.Count)
                SwapValue(gates[index - 1].Inputs, index, 0); // unlink from a gate
            else if (index < 0 && -index <= data.OutputIndices.Count)
                data.OutputIndices[-index - 1] = 0; // unlink from a output port of the circuit
        }

        // and the gate that replaced it
        var oldIndex = gates.Count;
        gate = gates[args.Index];
        foreach (var index in gate.Inputs)
        {
            if (index > 0 && index <= gates.Count)
                SwapValue(gates[index - 1].LinkedOutputs, oldIndex, args.Index);
            else if (index < 0 && -index <= circuit.Comp.LinkedInputs.Count)
                SwapValue(circuit.Comp.LinkedInputs[-index - 1], oldIndex, args.Index);
        }

        foreach (var index in gate.LinkedOutputs)
        {
            if (index > 0 && index <= gates.Count)
                SwapValue(gates[index - 1].Inputs, oldIndex, args.Index);
            else if (index < 0 && -index <= data.OutputIndices.Count)
                data.OutputIndices[-index - 1] = args.Index;
        }
        UpdateUI(ent);
    }

    private void OnLink(Entity<CircuitEditorComponent> ent, ref CircuitEditorLinkMessage args)
    {
        if (GetCircuit(ent) is not { } circuit)
            return;

        var data = circuit.Comp.Data;
        var gates = data.Gates;
        var i = args.Index;
        if (i == 0 || args.Input == 0 || (args.Input > 0 && args.Input > gates.Count) || (args.Input < 0 && -args.Input > data.OutputIndices.Count))
            return;

        if (i > 0)
        {
            if (i > gates.Count)
                return;

            var gate = gates[i - 1];
            if (args.N >= gate.Inputs.Count)
                return;

            gate.Inputs[args.N] = args.Input;
        }
        else
        {
            i = -i;
            if (i > data.OutputIndices.Count)
                return;

            data.OutputIndices[i - 1] = args.Input;
        }

        // already bounds checked at the top
        if (args.Input > 0)
            gates[args.Input - 1].LinkOutput(i);
        else
            circuit.Comp.LinkInput(-args.Input - 1, i);

        UpdateUI(ent);
    }

    private void OnUnlink(Entity<CircuitEditorComponent> ent, ref CircuitEditorUnlinkMessage args)
    {
        if (GetCircuit(ent) is not { } circuit)
            return;

        var data = circuit.Comp.Data;
        var gates = data.Gates;
        var i = args.Index;
        if (i == 0)
            return;

        var old = 0;
        if (i > 0)
        {
            if (i > gates.Count)
                return;

            var gate = gates[i - 1];
            if (args.N >= gate.Inputs.Count)
                return;

            old = gate.Inputs[args.N];
            gate.Inputs[args.N] = 0;
        }
        else
        {
            i = -i - 1;
            if (i >= data.OutputIndices.Count)
                return;

            old = data.OutputIndices[i];
            data.OutputIndices[i] = 0;
        }

        // clean up backreferences
        if (old > 0 && old <= gates.Count)
            gates[old - 1].LinkedOutputs.Remove(args.Index);
        else if (old < 0 && -old <= circuit.Comp.LinkedInputs.Count)
            circuit.Comp.LinkedInputs[-old - 1].Remove(args.Index);

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
    }

    public void UpdateUI(Entity<CircuitEditorComponent> ent)
    {
        var data = GetCircuit(ent)?.Comp.Data;
        var state = new CircuitEditorState(data);
        _ui.SetUiState(ent.Owner, CircuitEditorUiKey.Key, state);
    }

    private static void SwapValue(List<int> list, int from, int to)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == from)
                list[i] = to;
        }
    }
}
