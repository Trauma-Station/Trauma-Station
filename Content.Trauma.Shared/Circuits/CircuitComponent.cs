// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Circuits;

/// <summary>
/// Component for an integrated circuit, which can be linked to other machines with a <see cref="CircuitHousingComponent"/>.
/// Gates reference eachother, inputs and outputs with a <see cref="CircuitIndex"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class CircuitComponent : Component
{
    /// <summary>
    /// Number of input and output ports.
    /// There need to be enough source and sink port prototypes for it.
    /// </summary>
    public const int PortsCount = 8;

    /// <summary>
    /// Maximum number of gates you can have.
    /// </summary>
    public const int MaxGates = 256;

    /// <summary>
    /// The current inputs to the circuit.
    /// </summary>
    [DataField(serverOnly: true)]
    public List<object> Inputs = new();

    /// <summary>
    /// The last outputs of the circuit.
    /// </summary>
    [DataField(serverOnly: true)]
    public List<object> LastOutputs = new();

    /// <summary>
    /// List of circuit output index for each input.
    /// Built dynamically from gates.
    /// </summary>
    [DataField(serverOnly: true)]
    public List<List<CircuitIndex>> LinkedInputs = new();

    /// <summary>
    /// The housing this circuit is inside, if any.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Housing;

    /// <summary>
    /// Gates which have changed their output this tick.
    /// </summary>
    [DataField(serverOnly: true)]
    public HashSet<int> Changed = new();

    /// <summary>
    /// Data of the circuit programmed by a circuit editor console.
    /// </summary>
    [DataField(serverOnly: true)]
    public CircuitData Data = new();

    /// <summary>
    /// Get the value of a <see cref="CircuitGate"/> depending on a circuit index, falling back to false if invalid.
    /// </summary>
    public object GetValue(CircuitIndex idx)
    {
        if (idx.GateIndex is { } g)
            return Data.Gates.TryGetValue(g, out var gate) ? gate.Output : False.Instance;
        if (idx.PortIndex is { } p)
            return Inputs.TryGetValue(p, out var input) ? input : False.Instance;

        return False.Instance;
    }

    /// <summary>
    /// <see cref="GetValue"/> then get a boolean value for it.
    /// Strings are not supported, nonzero ints map to 1.
    /// </summary>
    public bool GetBool(CircuitIndex i)
    {
        switch (GetValue(i))
        {
            case True t:
                return true;
            case False f:
                return false;
            case Pulse p:
                return true;
            case Integer n:
                return n.Value != 0;
            default:
                return false;
        }
    }

    /// <summary>
    /// <see cref="GetValue"/> then get an int for it.
    /// Strings are not supported, bools map to 0/1.
    /// </summary>
    public int GetInt(CircuitIndex i)
    {
        switch (GetValue(i))
        {
            case True t:
                return 1;
            case False f:
                return 0;
            case Pulse p:
                return 1;
            case Integer n:
                return n.Value;
            default:
                return 0;
        }
    }

    /// <summary>
    /// Get the string representation of any value from <see cref="GetValue"/>.
    /// </summary>
    public string GetString(CircuitIndex i)
        => GetValue(i).ToString()!;

    /// <summary>
    /// Ensures that all port-related fields have at least <see cref="PortsCount"/> items.
    /// </summary>
    public void ValidatePortsCount()
    {
        // ensure required input port data exists
        var count = CircuitComponent.PortsCount;
        while (Inputs.Count < count)
            Inputs.Add(False.Instance);
        while (LinkedInputs.Count < count)
            LinkedInputs.Add(new());
        while (LastOutputs.Count < count)
            LastOutputs.Add(False.Instance);
        while (Data.OutputIndices.Count < count)
            Data.OutputIndices.Add(CircuitIndex.Invalid);
    }

    /// <summary>
    /// Backreference a link between an input and an output index.
    /// </summary>
    public void LinkOutput(CircuitIndex input, CircuitIndex output)
    {
        if (!Data.ValidIndex(input))
            return;

        if (input.GateIndex is { } g)
            Data.Gates[g].LinkOutput(output);
        else if (input.PortIndex is { } p)
            LinkInputPort(p, output);
    }

    /// <summary>
    /// Link all gate outputs together.
    /// </summary>
    public void LinkGateOutputs()
    {
        for (var i = 0; i < Data.Gates.Count; i++)
        {
            var gate = Data.Gates[i];
            var output = CircuitIndex.Gate(i);
            foreach (var input in gate.Inputs)
            {
                LinkOutput(input, output);
            }
        }

        for (var i = 0; i < Data.OutputIndices.Count; i++)
        {
            var input = Data.OutputIndices[i];
            LinkOutput(input, CircuitIndex.Port(i));
        }
    }

    /// <summary>
    /// Remove a link backreference between an input and an output index.
    /// </summary>
    public void UnlinkOutput(CircuitIndex input, CircuitIndex output)
    {
        if (!Data.ValidIndex(input))
            return;

        if (input.GateIndex is { } g)
            Data.Gates[g].LinkedOutputs.Remove(output);
        else if (input.PortIndex is { } p)
            UnlinkInputPort(p, output);
    }

    private void LinkInputPort(int i, CircuitIndex output)
    {
        if (LinkedInputs.TryGetValue(i, out var list) && !list.Contains(output))
            list.Add(output);
    }

    private void UnlinkInputPort(int i, CircuitIndex output)
    {
        if (LinkedInputs.TryGetValue(i, out var list))
            list.Remove(output);
    }
}

[DataRecord, Serializable, NetSerializable]
public sealed partial class CircuitData
{
    /// <summary>
    /// For each output port, which gate is used to find its value.
    /// 0 if it's not linked to anything.
    /// </summary>
    [ViewVariables]
    public List<CircuitIndex> OutputIndices = new();

    /// <summary>
    /// Each gate in the circuit.
    /// </summary>
    [ViewVariables]
    public List<CircuitGate> Gates = new();

    /// <summary>
    /// Returns true if an index is currently valid for this circuit data.
    /// </summary>
    public bool ValidIndex(CircuitIndex idx)
        => idx.GateIndex is {} g && g < Gates.Count
            || idx.PortIndex is {} p && p < CircuitComponent.PortsCount;
}
