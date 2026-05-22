// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Factory.Filters;
using Content.Shared.DeviceLinking;

namespace Content.Trauma.Shared.Circuits;

/// <summary>
/// Types of values a circuit gate can work with.
/// </summary>
[Serializable, NetSerializable]
public enum GateValue : byte
{
    Bool,
    Int,
    String,
    Any
}

/// <summary>
/// A momentary signal pulse which gets changed to false on the next tick.
/// </summary>
[Serializable, NetSerializable]
public sealed class Pulse
{
    /// <summary>
    /// Instance used when handling signal state.
    /// </summary>
    public static readonly Pulse Instance = new();
}

/// <summary>
/// Any kind of gate that can produce an output as part of a circuit in <see cref="CircuitData"/>.
/// </summary>
[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class CircuitGate
{
    /// <summary>
    /// Max distance from the center a gate can be placed at.
    /// </summary>
    public static readonly Vector2 MaxOffset = new Vector2(500f, 500f);

    /// <summary>
    /// The circuit input indices of this gate.
    /// </summary>
    [DataField]
    public List<CircuitIndex> Inputs = new();

    // have to make this nullable because serialization generator is dogshit and doesnt support just plain object
    [DataField("output")]
    protected object? _output = false;

    /// <summary>
    /// The last output of this gate.
    /// </summary>
    public object Output => _output ?? false;

    /// <summary>
    /// Where it is in the editor UI.
    /// </summary>
    [DataField]
    public Vector2 Pos = Vector2.Zero;

    /// <summary>
    /// Dynamically built circuit output indices which depend on this gate's output.
    /// </summary>
    [ViewVariables]
    public List<CircuitIndex> LinkedOutputs = new();

    /// <summary>
    /// Called after creating a new gate.
    /// </summary>
    public void Initialize()
    {
        _output = OutputType switch
        {
            GateValue.Bool => false,
            GateValue.Int => 0,
            GateValue.String => string.Empty,
            GateValue.Any => false,
            _ => false
        };
    }

    /// <summary>
    /// User-facing name of this gate
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Category used in the UI to sort gates.
    /// </summary>
    public abstract string Category { get; }

    /// <summary>
    /// Type of value this gate can output.
    /// </summary>
    public abstract GateValue OutputType { get; }

    /// <summary>
    /// How many inputs this gate has.
    /// </summary>
    public abstract int InputCount { get; }

    /// <summary>
    /// Update output based on inputs and other gates of a circuit.
    /// </summary>
    public abstract void Update(CircuitComponent comp);

    /// <summary>
    /// Add all variants of this gate to a list of gates.
    /// If there are no variants it just adds itself.
    /// </summary>
    public virtual void AddVariants(List<CircuitGate> gates)
    {
        gates.Add(this);
    }

    /// <summary>
    /// Called for a user's serialized gates.
    /// </summary>
    public void Validate()
    {
        Pos = ClampPosition(Pos);
        var count = InputCount;
        if (Inputs.Count > count)
            Inputs.RemoveRange(count, Inputs.Count - count);

        while (Inputs.Count < count)
            Inputs.Add(CircuitIndex.Invalid);
    }

    /// <summary>
    /// Add a linked circuit input index this gate is outputting to.
    /// </summary>
    public void LinkOutput(CircuitIndex linked)
    {
        if (!LinkedOutputs.Contains(linked))
            LinkedOutputs.Add(linked);
    }

    /// <summary>
    /// Clamp a gate position to the allowed range.
    /// </summary>
    public static Vector2 ClampPosition(Vector2 pos)
        => Vector2.Clamp(pos, -MaxOffset, MaxOffset);
}

/// <summary>
/// Stores any value when second input is true.
/// Output is always the stored value.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CircuitMemoryCell : CircuitGate
{
    public override string Name => "MEM";
    public override string Category => "Misc";
    public override GateValue OutputType => GateValue.Any;
    public override int InputCount => 2;

    public override void Update(CircuitComponent comp)
    {
        if (comp.GetBool(Inputs[1]))
            _output = comp.GetValue(Inputs[0]);
    }
}

/// <summary>
/// A binary logic gate for a circuit.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CircuitLogicGate : CircuitGate
{
    /// <summary>
    /// The binary logic operation to do on the inputs.
    /// </summary>
    [DataField]
    public LogicGate Gate = LogicGate.Or;

    public override string Name => Gate.ToString().ToUpper();
    public override string Category => "Boolean Logic";
    public override GateValue OutputType => GateValue.Bool;
    public override int InputCount => 2;

    public override void Update(CircuitComponent comp)
    {
        var a = comp.GetBool(Inputs[0]);
        var b = comp.GetBool(Inputs[1]);
        _output = Gate switch
        {
            LogicGate.Or => a || b,
            LogicGate.And => a && b,
            LogicGate.Xor => a != b,
            LogicGate.Nor => !(a || b),
            LogicGate.Nand => !(a && b),
            LogicGate.Xnor => a == b,
            _ => false
        };
    }

    public override void AddVariants(List<CircuitGate> gates)
    {
        var values = (LogicGate[]) Enum.GetValues(typeof(LogicGate));
        foreach (var gate in values)
        {
            gates.Add(new CircuitLogicGate()
            {
                Gate = gate
            });
        }
    }
}

/// <summary>
/// Unary gate that gets the length of a string.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CircuitStrLenGate : CircuitGate
{
    public override string Name => "LEN";
    public override string Category => "Strings";
    public override GateValue OutputType => GateValue.Int;
    public override int InputCount => 1;

    public override void Update(CircuitComponent comp)
    {
        var s = comp.GetString(Inputs[0]);
        _output = s.Length;
    }
}

/// <summary>
/// Unary gate which compares the second input string against the first input string.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CircuitStrCompareGate : CircuitGate
{
    [DataField]
    public NameFilterMode Mode = NameFilterMode.Contain;

    public override string Name => Mode.ToString().ToUpper();
    public override string Category => "Strings";
    public override GateValue OutputType => GateValue.Bool;
    public override int InputCount => 1;

    public override void Update(CircuitComponent comp)
    {
        var s = comp.GetString(Inputs[0]);
        var check = comp.GetString(Inputs[1]);
        _output = Mode switch
        {
            NameFilterMode.Contain => s.Contains(check),
            NameFilterMode.Start => s.StartsWith(check),
            NameFilterMode.End => s.EndsWith(check),
            NameFilterMode.Match => s == check,
            _ => false
        };
    }

    public override void AddVariants(List<CircuitGate> gates)
    {
        var modes = (NameFilterMode[]) Enum.GetValues(typeof(NameFilterMode));
        foreach (var mode in modes)
        {
            gates.Add(new CircuitStrCompareGate()
            {
                Mode = mode
            });
        }
    }
}

/// <summary>
/// Binary math gate, operating on 2 int inputs.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CircuitMathGate : CircuitGate
{
    [DataField]
    public MathOp Op = MathOp.Add;

    public override string Name => Op.ToString().ToUpper();
    public override string Category => "Maths";
    public override GateValue OutputType => GateValue.Int;
    public override int InputCount => 2;

    public override void Update(CircuitComponent comp)
    {
        var a = comp.GetInt(Inputs[0]);
        var b = comp.GetInt(Inputs[1]);
        _output = Op switch
        {
            // arithmetic
            MathOp.Add => a + b,
            MathOp.Sub => a - b,
            MathOp.Mul => a * b,
            MathOp.Div => a / b,
            MathOp.Rem => a % b,
            // bitwise
            MathOp.Bor => a | b,
            MathOp.Band => a & b,
            MathOp.Bxor => a ^ b,
            MathOp.Bls => a << b,
            MathOp.Brs => a >> b,
            _ => 0
        };
    }

    public override void AddVariants(List<CircuitGate> gates)
    {
        var ops = (MathOp[]) Enum.GetValues(typeof(MathOp));
        foreach (var op in ops)
        {
            gates.Add(new CircuitMathGate()
            {
                Op = op
            });
        }
    }
}

[Serializable, NetSerializable]
public enum MathOp : byte
{
    Add,
    Sub,
    Mul,
    Div,
    Rem,
    Bor,
    Band,
    Bxor,
    Bls,
    Brs
}

/// <summary>
/// Binary comparison gate, operating on 2 int inputs.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CircuitCompareGate : CircuitGate
{
    [DataField]
    public CompareOp Op = CompareOp.Equal;

    public override string Name => Op switch
    {
        CompareOp.Equal => "==",
        CompareOp.NotEqual => "!=",
        CompareOp.Greater => ">",
        CompareOp.GreaterEqual => ">=",
        CompareOp.Less => "<",
        CompareOp.LessEqual => "<=",
        _ => "?"
    };
    public override string Category => "Integers";
    public override GateValue OutputType => GateValue.Bool;
    public override int InputCount => 2;

    public override void Update(CircuitComponent comp)
    {
        var a = comp.GetInt(Inputs[0]);
        var b = comp.GetInt(Inputs[1]);
        _output = Op switch
        {
            CompareOp.Equal => a == b,
            CompareOp.NotEqual => a != b,
            CompareOp.Greater => a > b,
            CompareOp.GreaterEqual => a >= b,
            CompareOp.Less => a < b,
            CompareOp.LessEqual => a <= b,
            _ => false
        };
    }

    public override void AddVariants(List<CircuitGate> gates)
    {
        var ops = (CompareOp[]) Enum.GetValues(typeof(CompareOp));
        foreach (var op in ops)
        {
            gates.Add(new CircuitCompareGate()
            {
                Op = op
            });
        }
    }
}

[Serializable, NetSerializable]
public enum CompareOp : byte
{
    Equal,
    NotEqual,
    Greater,
    GreaterEqual,
    Less,
    LessEqual
}
