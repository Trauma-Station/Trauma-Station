// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using System.Text;

namespace Content.Trauma.Shared.ChemiCompiler;

/// <summary>
/// Added to a <see cref="ChemiCompilerComponent"/> while it is running a program, and holds everything
/// that program is doing. Removed the moment it halts, so idle machines never get looked at by the update loop.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class ActiveChemiCompilerComponent : Component
{
    /// <summary>
    /// Which code slot is running.
    /// </summary>
    [DataField(serverOnly: true)]
    public int Slot;

    /// <summary>
    /// The program being run. Copied so editing the slot mid-run can't change what's executing.
    /// </summary>
    [DataField(serverOnly: true)]
    public string Program = string.Empty;

    /// <summary>
    /// Matching bracket index for every instruction, see <see cref="ChemFuck.BuildJumpTable"/>.
    /// </summary>
    [DataField(serverOnly: true)]
    public int[] JumpTable = [];

    /// <summary>
    /// The program's memory, <see cref="ChemiCompilerComponent.MemorySize"/> single byte cells.
    /// </summary>
    [DataField(serverOnly: true)]
    public byte[] Memory = new byte[ChemiCompilerComponent.MemorySize];

    /// <summary>
    /// Data pointer, indexes into <see cref="Memory"/>.
    /// </summary>
    [DataField(serverOnly: true)]
    public int Pointer;

    /// <summary>
    /// Instruction pointer, indexes into <see cref="Program"/>.
    /// </summary>
    [DataField(serverOnly: true)]
    public int Instruction;

    /// <summary>
    /// Source register, which reservoir instructions read from.
    /// </summary>
    [DataField(serverOnly: true)]
    public int Source;

    /// <summary>
    /// Target register, where instructions put things.
    /// Reservoirs 1 to 10, or the pill, vial and ejection ports above that.
    /// </summary>
    [DataField(serverOnly: true)]
    public int Target;

    /// <summary>
    /// Amount register, how many units instructions move and how far <see cref="ChemFuck.Heat"/> heats.
    /// </summary>
    [DataField(serverOnly: true)]
    public int Amount;

    /// <summary>
    /// Text written so far by <see cref="ChemFuck.Output"/>.
    /// </summary>
    public StringBuilder Output = new();

    /// <summary>
    /// How many instructions have run, checked against <see cref="ChemiCompilerComponent.MaxInstructions"/>.
    /// </summary>
    [DataField(serverOnly: true)]
    public int Executed;

    /// <summary>
    /// When this run began, checked against <see cref="ChemiCompilerComponent.MaxRuntime"/>.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan Started;

    /// <summary>
    /// When the machine may run its next instruction.
    /// Every instruction sets this, so the machine works through a program at a visible pace.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextStep;

    /// <summary>
    /// Temperature <see cref="ChemFuck.Heat"/> is working towards, applied once <see cref="NextStep"/> passes.
    /// Null when nothing is being heated.
    /// </summary>
    [DataField(serverOnly: true)]
    public float? PendingHeat;

    /// <summary>
    /// Which reservoir <see cref="PendingHeat"/> applies to, since the source register may have moved on by then.
    /// </summary>
    [DataField(serverOnly: true)]
    public int PendingHeatSource;

    /// <summary>
    /// What the reservoir was at when heating began, so the temperature can be walked from there to the target.
    /// </summary>
    [DataField(serverOnly: true)]
    public float HeatStartTemperature;

    /// <summary>
    /// When heating began. Together with <see cref="NextStep"/> this gives how far along the heating is.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan HeatStart;

    /// <summary>
    /// When the machine may complain about a failed instruction again.
    /// A loop full of failing instructions should sound annoyed, not deafening.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextFailSound;
}
