// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using static Robust.Shared.Serialization.Manager.ISerializationManager;

namespace Content.Trauma.Shared.Circuits;

/// <summary>
/// Wrapper for a circuit index which can reference a gate of the circuit or sink/source port of the housing.
/// If the index is 0, the reference is invalid.
/// If the index is positive, it's a 1-based index for a gate.
/// If the index is negative, it's a 1-based index for a sink/source port of the housing, depending on context.
/// </summary>
[Serializable, NetSerializable]
public readonly record struct CircuitIndex(int Raw = 0)
{
    public static readonly CircuitIndex Invalid = new();

    public static CircuitIndex Gate(int i)
        => new(i + 1);

    public static CircuitIndex Port(int i)
        => new(-i - 1);

    public bool Valid => Raw != 0;
    /// <summary>
    /// Whether this index refers to a gate on the circuit.
    /// </summary>
    public bool IsGate => Raw > 0;
    /// <summary>
    /// Whether this index refers to a port of the housing.
    /// Input/output depends on the context used.
    /// </summary>
    public bool IsPort => Raw < 0;

    /// <summary>
    /// Get a 0-based index for a circuit's gate, or null for non-gate values.
    /// </summary>
    public int? GateIndex => IsGate ? Raw - 1 : null;

    /// <summary>
    /// Get a 0-based index for a housing's port, or null for non-port values.
    /// </summary>
    public int? PortIndex => IsPort ? -Raw - 1 : null;

    public override string ToString()
    {
        if (GateIndex is { } g)
            return g.ToString();

        return PortIndex is { } p
            ? $"p{p}"
            : "i";
    }
}

/// <summary>
/// Serializes gate indices as the number itself, prepends "p" for port indices.
/// Invalid values use the string "i"
/// </summary>
[TypeSerializer]
public sealed class CircuitIndexSerializer : ITypeSerializer<CircuitIndex, ValueDataNode>, ITypeCopyCreator<CircuitIndex>
{
    public CircuitIndex Read(ISerializationManager ser,
        ValueDataNode node,
        IDependencyCollection deps,
        SerializationHookContext hookCtx,
        ISerializationContext? ctx = null,
        InstantiationDelegate<CircuitIndex>? instanceProvider = null)
    {
        var data = node.Value;
        return data[0] switch
        {
            'i' => CircuitIndex.Invalid,
            'p' => CircuitIndex.Port(int.Parse(data[1..])),
            _ => CircuitIndex.Gate(int.Parse(data))
        };
    }

    public DataNode Write(ISerializationManager ser,
        CircuitIndex idx,
        IDependencyCollection deps,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return new ValueDataNode(idx.ToString());
    }

    public ValidationNode Validate(ISerializationManager ser,
        ValueDataNode node,
        IDependencyCollection deps,
        ISerializationContext? ctx = null)
    {
        var data = node.Value;
        if (data.Length == 0)
            return new ErrorNode(node, "CircuitIndex value cannot be empty");

        var first = data[0];
        if (first == 'i')
            return data.Length == 1
                ? new ErrorNode(node, $"An invalid CircuitIndex must only be the string 'i', found '{data}'")
                : new ValidatedValueNode(node);

        var number = data;
        if (first == 'p')
            number = data[1..]; // skip first char for ports
        return int.TryParse(number, out _)
            ? new ErrorNode(node, $"Failed to parse integer for CircuitIndex '{data}'")
            : new ValidatedValueNode(node);
    }

    public CircuitIndex CreateCopy(ISerializationManager ser,
        CircuitIndex idx,
        IDependencyCollection deps,
        SerializationHookContext hookCtx,
        ISerializationContext? ctx)
    {
        return new(idx.Raw);
    }
}
