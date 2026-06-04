// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DeviceLinking;
using Content.Goobstation.Shared.Factory.Filters;

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

// need these wrapper classes because reflection manager yml tag lookup is broken and refuses to use structs like Int32 or Boolean...
[DataDefinition, Serializable, NetSerializable]
public sealed partial class True
{
    public override string ToString() => "true";

    public static readonly True Instance = new();
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class False
{
    public override string ToString() => "false";

    public static readonly False Instance = new();
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class Pulse
{
    public override string ToString() => "pulse";

    public static readonly Pulse Instance = new();
}

[DataRecord, Serializable, NetSerializable]
public sealed partial class Integer
{
    public Integer()
    {
    }

    public Integer(int value)
    {
        Value = value;
    }

    public int Value;

    public override string ToString()
        => Value.ToString();

    public static readonly Integer Zero = new();
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

    // TODO: make custom serializer for it
    /// <summary>
    /// Only these C# types are allowed for output values.
    /// </summary>
    private static Type[] AllowedTypes =
    [
        typeof(True),
        typeof(False),
        typeof(Pulse),
        typeof(Integer),
        typeof(String)
    ];

    /// <summary>
    /// The circuit input indices of this gate.
    /// </summary>
    [DataField]
    public List<CircuitIndex> Inputs = new();

    // have to make this nullable because serialization generator is dogshit and doesnt support just plain object
    [DataField("output")]
    private object? _output;

    /// <summary>
    /// The last output of this gate.
    /// </summary>
    public object Output => _output ?? False.Instance;

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
    /// Set up a valid output value for this gate.
    /// Called after creating a new gate or importing a circuit.
    /// </summary>
    public void Initialize()
    {
        if (_output?.GetType() is { } type && !AllowedTypes.Contains(type))
            _output = null;
        _output ??= OutputType switch
        {
            GateValue.Int => Integer.Zero,
            GateValue.String => string.Empty,
            _ => False.Instance
        };
    }

    /// <summary>
    /// User-facing name of this gate
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Category used in the UI to sort gates.
    /// If this is empty, it will not be listed in the picker.
    /// </summary>
    public abstract string Category { get; }

    /// <summary>
    /// User-facing tooltop if this gate, shown on hover.
    /// </summary>
    public abstract string Desc { get; }

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

    protected void SetOutput(bool value)
    {
        _output = value ? True.Instance : False.Instance;
    }

    protected void SetOutput(int value)
    {
        if (_output is Integer existing)
            existing.Value = value;
        else
            _output = new Integer(value);
    }

    protected void SetOutput(string value)
    {
        _output = value;
    }

    protected void SetOutputToObject(object value)
    {
        _output = value;
    }

    protected void CopyOutputFromObject(object value)
    {
        if (value is Integer integer)
            _output = new Integer(integer.Value); // only datatype that needs to be deep copied is the int wrapper
        else
            _output = value; // everything else has no data or can be passed as-is (string)
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
/// A constant value source.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CircuitConstantGate : CircuitGate
{
    public CircuitConstantGate(object value)
    {
        SetOutputToObject(value);
    }

    public override string Name => "CONST";
    public override string Category => string.Empty; // UI has a dedicated thing to add constants, hide it
    public override string Desc => $"Always has a constant output value: {Output}";
    public override GateValue OutputType => GateValue.Any;
    public override int InputCount => 0;

    public override void Update(CircuitComponent comp) {}
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
    public override string Desc => "Always outputs the value stored in memory.\nIf the second input is set to true, stores the first input to memory.";
    public override int InputCount => 2;

    public override void Update(CircuitComponent comp)
    {
        if (comp.GetBool(Inputs[1]))
            CopyOutputFromObject(comp.GetValue(Inputs[0]));
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
    public override string Desc => Gate switch
    {
        LogicGate.Or => "True if at least 1 input is true",
        LogicGate.And => "True if both inputs are true",
        LogicGate.Xor => "True if the inputs are different",
        LogicGate.Nor => "True if both inputs are false",
        LogicGate.Nand => "True if at most 1 input is true",
        LogicGate.Xnor => "True if the inputs are the same",
        _ => string.Empty
    };
    public override GateValue OutputType => GateValue.Bool;
    public override int InputCount => 2;

    public override void Update(CircuitComponent comp)
    {
        var a = comp.GetBool(Inputs[0]);
        var b = comp.GetBool(Inputs[1]);
        SetOutput(Gate switch
        {
            LogicGate.Or => a || b,
            LogicGate.And => a && b,
            LogicGate.Xor => a != b,
            LogicGate.Nor => !(a || b),
            LogicGate.Nand => !(a && b),
            LogicGate.Xnor => a == b,
            _ => false
        });
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
    public override string Desc => "Outputs the input string's length as an integer";
    public override GateValue OutputType => GateValue.Int;
    public override int InputCount => 1;

    public override void Update(CircuitComponent comp)
    {
        var s = comp.GetString(Inputs[0]);
        SetOutput(s.Length);
    }
}

/// <summary>
/// Binary gate that gets the integer value of the nth char of a string.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CircuitStrCharGate : CircuitGate
{
    public override string Name => "CHAR";
    public override string Category => "Strings";
    public override string Desc => "First input: string, second input: int, output: int value of the string at that position";
    public override GateValue OutputType => GateValue.Int;
    public override int InputCount => 2;

    public override void Update(CircuitComponent comp)
    {
        var s = comp.GetString(Inputs[0]);
        var i = comp.GetInt(Inputs[1]);
        SetOutput(i < s.Length ? (int) s[i] : 0);
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
    public override string Desc => Mode switch
    {
        NameFilterMode.Contain => "True if the first input string contains the second input string",
        NameFilterMode.Start => "True if the first input string starts with the second input string",
        NameFilterMode.End => "True if the first input string ends with the second input string",
        NameFilterMode.Match => "True if the first input string is the same as the second input string",
        _ => string.Empty
    };
    public override GateValue OutputType => GateValue.Bool;
    public override int InputCount => 2;

    public override void Update(CircuitComponent comp)
    {
        var s = comp.GetString(Inputs[0]);
        var check = comp.GetString(Inputs[1]);
        SetOutput(Mode switch
        {
            NameFilterMode.Contain => s.Contains(check),
            NameFilterMode.Start => s.StartsWith(check),
            NameFilterMode.End => s.EndsWith(check),
            NameFilterMode.Match => s == check,
            _ => false
        });
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
/// Unary gate that makes a string lowercase.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CircuitStrLowerGate : CircuitGate
{
    public override string Name => "LOWER";
    public override string Category => "Strings";
    public override string Desc => "Makes a string lowercase";
    public override GateValue OutputType => GateValue.String;
    public override int InputCount => 1;

    public override void Update(CircuitComponent comp)
    {
        var s = comp.GetString(Inputs[0]);
        SetOutput(s.ToLower());
    }
}

/// <summary>
/// Unary gate that makes a string uppercase.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CircuitStrUpperGate : CircuitGate
{
    public override string Name => "UPPER";
    public override string Category => "Strings";
    public override string Desc => "Makes a string uppercase";
    public override GateValue OutputType => GateValue.String;
    public override int InputCount => 1;

    public override void Update(CircuitComponent comp)
    {
        var s = comp.GetString(Inputs[0]);
        SetOutput(s.ToUpper());
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
    public override string Desc => Op switch
    {
        // if you need this you need to go to school...
        MathOp.Add => "Sum of the inputs",
        MathOp.Sub => "First input minus the second input",
        MathOp.Mul => "Prodcut of the inputs",
        MathOp.Div => "First input divided by the second input",
        MathOp.Rem => "Remainder of dividing the first input by the second input",
        MathOp.Bor => "Bitwise OR of the inputs",
        MathOp.Band => "Bitwise AND of the inputs",
        MathOp.Bxor => "Bitwise XOR of the inputs",
        MathOp.Bls => "Shift the first input left by the second input bits",
        MathOp.Brs => "Shift the first input right by the second input bits",
        _ => string.Empty
    };
    public override GateValue OutputType => GateValue.Int;
    public override int InputCount => 2;

    public override void Update(CircuitComponent comp)
    {
        var a = comp.GetInt(Inputs[0]);
        var b = comp.GetInt(Inputs[1]);
        SetOutput(Op switch
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
        });
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
    public override string Desc => Op switch
    {
        CompareOp.Equal => "True if the input integers are equal",
        CompareOp.NotEqual => "True if the input integers are not equal",
        CompareOp.Greater => "True if the first input is greater than the second input",
        CompareOp.GreaterEqual => "True if the first input is greather than or equal to the second input",
        CompareOp.Less => "True if the first input is less than the second input",
        CompareOp.LessEqual => "Ttrue if the first input is less than or equal to the second input",
        _ => string.Empty
    };
    public override GateValue OutputType => GateValue.Bool;
    public override int InputCount => 2;

    public override void Update(CircuitComponent comp)
    {
        var a = comp.GetInt(Inputs[0]);
        var b = comp.GetInt(Inputs[1]);
        SetOutput(Op switch
        {
            CompareOp.Equal => a == b,
            CompareOp.NotEqual => a != b,
            CompareOp.Greater => a > b,
            CompareOp.GreaterEqual => a >= b,
            CompareOp.Less => a < b,
            CompareOp.LessEqual => a <= b,
            _ => false
        });
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
