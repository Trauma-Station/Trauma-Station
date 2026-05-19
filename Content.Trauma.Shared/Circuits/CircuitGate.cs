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
    public List<int> Inputs = new();

    /// <summary>
    /// The last output of this gate.
    /// </summary>
    [DataField]
    public object Output = false;

    /// <summary>
    /// Where it is in the editor UI.
    /// </summary>
    [DataField]
    public Vector2 Pos = Vector2.Zero;

    /// <summary>
    /// Dynamically built circuit output indices that depend on this gate's output.
    /// </summary>
    [ViewVariables, NonSerialized]
    public List<int> LinkedOutputs = new();

    /// <summary>
    /// Called after creating a new gate.
    /// </summary>
    public void Initialize()
    {
        Inputs = new(InputCount());
        Output = OutputType() switch
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
    public abstract string Name();

    /// <summary>
    /// Type of value this gate can output.
    /// </summary>
    public abstract GateValue OutputType();

    /// <summary>
    /// How many inputs this gate has.
    /// </summary>
    public abstract int InputCount();

    /// <summary>
    /// Update output based on inputs and other gates of a circuit.
    /// </summary>
    public abstract void Update(CircuitComponent comp);

    /// <summary>
    /// Called for a user's serialized gates.
    /// </summary>
    public void Validate()
    {
        Pos = Vector2.Clamp(Pos, -MaxOffset, MaxOffset);
        var count = InputCount();
        if (Inputs.Count > count)
            Inputs.RemoveRange(count, Inputs.Count - count);
        else if (Inputs.Count == count)
            return;

        while (Inputs.Count < count)
            Inputs.Add(0);
    }

    /// <summary>
    /// Add a linked circuit input index this gate is outputting to.
    /// </summary>
    public void LinkOutput(int linked)
    {
        if (!LinkedOutputs.Contains(linked))
            LinkedOutputs.Add(linked);
    }
}

/// <summary>
/// Stores any value when second input is true.
/// Output is always the stored value.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CircuitMemoryCell : CircuitGate
{
    public override string Name() => "MEM";

    public override GateValue OutputType() => GateValue.Any;

    public override int InputCount() => 2;

    public override void Update(CircuitComponent comp)
    {
        if (comp.GetBool(Inputs[1]))
            Output = comp.GetValue(Inputs[0]);
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

    public override string Name() => Gate.ToString().ToUpper();

    public override GateValue OutputType() => GateValue.Bool;

    public override int InputCount() => 2;

    public override void Update(CircuitComponent comp)
    {
        var a = comp.GetBool(Inputs[0]);
        var b = comp.GetBool(Inputs[1]);
        Output = Gate switch
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
}

/// <summary>
/// Unary gate that gets the length of a string.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CircuitStrLenGate : CircuitGate
{
    public override string Name() => "LEN";

    public override GateValue OutputType() => GateValue.Int;

    public override int InputCount() => 1;

    public override void Update(CircuitComponent comp)
    {
        var s = comp.GetValue(Inputs[0]).ToString();
        Output = s.Length;
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

    public override string Name() => Mode.ToString().ToUpper();

    public override GateValue OutputType() => GateValue.Bool;

    public override int InputCount() => 1;

    public override void Update(CircuitComponent comp)
    {
        var s = comp.GetValue(Inputs[0]).ToString();
        var check = comp.GetValue(Inputs[1]).ToString();
        Output = Mode switch
        {
            NameFilterMode.Contain => s.Contains(check),
            NameFilterMode.Start => s.StartsWith(check),
            NameFilterMode.End => s.EndsWith(check),
            NameFilterMode.Match => s == check,
            _ => false
        };
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

    public override string Name() => Op.ToString().ToUpper();

    public override GateValue OutputType() => GateValue.Int;

    public override int InputCount() => 2;

    public override void Update(CircuitComponent comp)
    {
        var a = comp.GetInt(Inputs[0]);
        var b = comp.GetInt(Inputs[1]);
        Output = Op switch
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

    public override string Name() => Op switch
    {
        CompareOp.Equal => "==",
        CompareOp.NotEqual => "!=",
        CompareOp.Greater => ">",
        CompareOp.GreaterEqual => ">=",
        CompareOp.Less => "<",
        CompareOp.LessEqual => "<="
    };

    public override GateValue OutputType() => GateValue.Bool;

    public override int InputCount() => 2;

    public override void Update(CircuitComponent comp)
    {
        var a = comp.GetInt(Inputs[0]);
        var b = comp.GetInt(Inputs[1]);
        Output = Op switch
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
