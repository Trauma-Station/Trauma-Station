// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.ChemiCompiler;

/// <summary>
/// Every instruction the ChemiCompiler understands.
/// Anything not in here is ignored by the interpreter so programs can have whitespace and comments in them.
/// </summary>
public static class ChemFuck
{
    public const char PointerRight = '>';
    public const char PointerLeft = '<';
    public const char Increment = '+';
    public const char Decrement = '-';
    public const char LoopStart = '[';
    public const char LoopEnd = ']';
    public const char StoreSource = '}';
    public const char LoadSource = '{';
    public const char StoreTarget = ')';
    public const char LoadTarget = '(';
    public const char StoreAmount = '\'';
    public const char LoadAmount = '^';
    public const char Measure = ',';
    public const char Heat = '$';
    public const char Transfer = '@';
    public const char Isolate = '#';
    public const char Output = '.';
    public const char Lock = '~';
    public const char Nop = '*';

    /// <summary>
    /// How long an instruction ties the machine up for.
    /// Arithmetic is cheap because it is the only way to write a number down in this language, and charging
    /// full price per + would mean a heat instruction spent most of its time counting.
    /// </summary>
    public enum Speed : byte
    {
        /// <summary>
        /// Pointer movement and arithmetic. Runs many times per program, so it is the cheapest tier.
        /// </summary>
        Fast,

        /// <summary>
        /// Register shuffling and bookkeeping.
        /// </summary>
        Normal,

        /// <summary>
        /// Anything that reaches into a beaker. The expensive tier, and the one that gates a program's output.
        /// </summary>
        Physical,

        /// <summary>
        /// Deliberately standing still, for when a program needs to give the chemistry a moment.
        /// </summary>
        Slow,
    }

    /// <summary>
    /// Which speed tier an instruction runs at.
    /// <see cref="Heat"/> works out its own, much longer wait, so its tier is never asked for.
    /// </summary>
    public static Speed SpeedOf(char c)
    {
        switch (c)
        {
            case PointerRight:
            case PointerLeft:
            case Increment:
            case Decrement:
            case LoopStart:
            case LoopEnd:
                return Speed.Fast;

            case Measure:
            case Transfer:
            case Isolate:
                return Speed.Physical;

            case Nop:
                return Speed.Slow;

            default:
                return Speed.Normal;
        }
    }

    /// <summary>
    /// Returns true if a character is an instruction, as opposed to padding the player added for readability.
    /// </summary>
    public static bool IsInstruction(char c)
    {
        switch (c)
        {
            case PointerRight:
            case PointerLeft:
            case Increment:
            case Decrement:
            case LoopStart:
            case LoopEnd:
            case StoreSource:
            case LoadSource:
            case StoreTarget:
            case LoadTarget:
            case StoreAmount:
            case LoadAmount:
            case Measure:
            case Heat:
            case Transfer:
            case Isolate:
            case Output:
            case Lock:
            case Nop:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Builds the table used to jump between matching <see cref="LoopStart"/> and <see cref="LoopEnd"/> instructions.
    /// Every index holds the index of its partner bracket, or -1 for anything that isn't a bracket.
    /// Precomputing this means <see cref="LoopEnd"/> doesn't have to scan backwards through the whole program every
    /// single iteration, which matters a lot when a loop runs thousands of times.
    /// </summary>
    /// <remarks>
    /// Returns null when the brackets don't match up. Refusing to run an unbalanced program is deliberate:
    /// there is nowhere sensible for an unmatched bracket to jump to, and hanging forever would brick the machine.
    /// </remarks>
    public static int[]? BuildJumpTable(string program)
    {
        var table = new int[program.Length];
        var open = new Stack<int>();

        for (var i = 0; i < program.Length; i++)
        {
            table[i] = -1;
            switch (program[i])
            {
                case LoopStart:
                    open.Push(i);
                    break;
                case LoopEnd:
                    if (!open.TryPop(out var start))
                        return null; // ] with no [ before it

                    table[start] = i;
                    table[i] = start;
                    break;
            }
        }

        // any [ left over never got closed
        return open.Count == 0 ? table : null;
    }

    /// <summary>
    /// Checks a program can actually be run, returning a localised error message if it can't.
    /// </summary>
    public static string? Validate(string program, int maxLength)
    {
        if (program.Length > maxLength)
            return Loc.GetString("chemicompiler-error-too-long", ("max", maxLength));

        var depth = 0;
        foreach (var c in program)
        {
            switch (c)
            {
                case LoopStart:
                    depth++;
                    break;
                case LoopEnd:
                    if (--depth < 0)
                        return Loc.GetString("chemicompiler-error-unmatched-end");
                    break;
            }
        }

        if (depth > 0)
            return Loc.GetString("chemicompiler-error-unmatched-start");

        return null;
    }
}
