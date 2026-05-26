// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Circuits;

/// <summary>
/// Component for circuit editor console that lets you use the BUI to edit inserted circuits.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(CircuitEditorSystem))]
public sealed partial class CircuitEditorComponent : Component
{
    [DataField]
    public string SlotId = "circuit";
}

[Serializable, NetSerializable]
public enum CircuitEditorUiKey : byte
{
    Key
}

/// <summary>
/// State for the circuit editor BUI.
/// Networking circuits to everyone would be way too expensive, no prediction :(
/// </summary>
[Serializable, NetSerializable]
public sealed class CircuitEditorState(CircuitData? data, NetEntity? circuit) : BoundUserInterfaceState
{
    public readonly CircuitData? Data = data;
    public readonly NetEntity? Circuit = circuit;
}

/// <summary>
/// Clears the entire circuit.
/// </summary>
[Serializable, NetSerializable]
public sealed class CircuitEditorClearMessage : BoundUserInterfaceMessage;

/// <summary>
/// Imports a complete circuit, clearing whatever was there before.
/// </summary>
[Serializable, NetSerializable]
public sealed class CircuitEditorImportMessage(CircuitData data) : BoundUserInterfaceMessage
{
    public readonly CircuitData Data = data;
}

/// <summary>
/// Try to add a new gate to the circuit.
/// </summary>
[Serializable, NetSerializable]
public sealed class CircuitEditorAddGateMessage(CircuitGate gate) : BoundUserInterfaceMessage
{
    public readonly CircuitGate Gate = gate;
}

/// <summary>
/// Move a 0-indexed gate to a position in the editor.
/// </summary>
[Serializable, NetSerializable]
public sealed class CircuitEditorMoveGateMessage(int index, Vector2 pos) : BoundUserInterfaceMessage
{
    public readonly int Index = index;
    public readonly Vector2 Pos = pos;
}

/// <summary>
/// Swap remove a gate with a given 0-based array index.
/// </summary>
[Serializable, NetSerializable]
public sealed class CircuitEditorRemoveGateMessage(int index) : BoundUserInterfaceMessage
{
    public readonly int Index = index;
}

/// <summary>
/// Link a gate input index to the nth input for a given gate output index.
/// For a circuit output port (negative value for input) n will be 0.
/// </summary>
[Serializable, NetSerializable]
public sealed class CircuitEditorLinkMessage(CircuitIndex input, CircuitIndex index, int n) : BoundUserInterfaceMessage
{
    public readonly CircuitIndex Input = input;
    public readonly CircuitIndex Index = index;
    public readonly int N = n;
}

/// <summary>
/// Unlink the nth input of a gate output index.
/// For a circuit output port (negative value for index) n will be 0.
/// </summary>
[Serializable, NetSerializable]
public sealed class CircuitEditorUnlinkMessage(CircuitIndex index, int n) : BoundUserInterfaceMessage
{
    public readonly CircuitIndex Index = index;
    public readonly int N = n;
}
