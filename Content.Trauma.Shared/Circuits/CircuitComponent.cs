// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Circuits;

/// <summary>
/// Component for an integrated circuit, which can be linked to other machines with a <see cref="CircuitHousingComponent"/>.
/// Gates reference eachother, inputs and outputs with a circuit value index.
/// If the index is 0, the reference is invalid.
/// If the index is positive, it's a 1-based index for a gate.
/// If the index is negative, it's a 1-based index for a sink/source port of the housing, depending on context.
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
    public List<List<int>> LinkedInputs = new();

    /// <summary>
    /// The housing this circuit is inside, or invalid if it's not in one.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Housing;

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
    /// Get the value of a <see cref="CircuitGate"/> if positive, or circuit input if negative, falling back to false if 0 (unlinked) or out of bounds.
    /// </summary>
    public object GetValue(int i)
        => i >= 0
            ? i > 0 && i <= Data.Gates.Count ? Data.Gates[i - 1].Output : false
            : -i <= Inputs.Count ? Inputs[-i - 1] : false;

    /// <summary>
    /// <see cref="GetValue"/> then get a boolean value for it.
    /// Strings are not supported, nonzero ints map to 1.
    /// </summary>
    public bool GetBool(int i)
    {
        switch (GetValue(i))
        {
            case bool b:
                return b;
            case Pulse:
                return true;
            case int n:
                return n != 0;
            default:
                return false;
        }
    }

    /// <summary>
    /// <see cref="GetValue"/> then get an int for it.
    /// Strings are not supported, bools map to 0/1.
    /// </summary>
    public int GetInt(int i)
    {
        switch (GetValue(i))
        {
            case bool b:
                return b ? 1 : 0;
            case Pulse:
                return 1;
            case int n:
                return n;
            default:
                return 0;
        }
    }

    /// <summary>
    /// Link a circuit output index as using a certain input.
    /// </summary>
    public void LinkInput(int i, int linked)
    {
        if (i < 0 || i >= LinkedInputs.Count)
            return;

        var list = LinkedInputs[i];
        if (!list.Contains(linked))
            list.Add(linked);
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
    public List<int> OutputIndices = new();

    /// <summary>
    /// Each gate in the circuit.
    /// </summary>
    [ViewVariables]
    public List<CircuitGate> Gates = new();
}
