// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Genetics.Console;

/// <summary>
/// Component for the genetics computer.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(GeneticsConsoleSystem))]
[AutoGenerateComponentState(fieldDeltas: true)]
[AutoGenerateComponentPause]
public sealed partial class GeneticsConsoleComponent : Component
{
    /// <summary>
    /// Subjects with more than this number of genetic damage can't be sequenced etc.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxGeneticDamage = 90;

    #region Scrambling

    /// <summary>
    /// How long you have to wait between scrambling mobs genomes.
    /// Starts when the computer is built to prevent cheesing.
    /// </summary>
    [DataField]
    public TimeSpan ScrambleCooldown = TimeSpan.FromSeconds(180);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan NextScramble = TimeSpan.Zero;

    /// <summary>
    /// Damage dealt to the mob when scrambling its genome.
    /// </summary>
    [DataField]
    public DamageSpecifier ScrambleDamage = new DamageSpecifier()
    {
        DamageDict = new()
        {
            { "Radiation", 50 },
            { "Cellular", 10 }
        }
    };

    #endregion

    #region Sequencing

    /// <summary>
    /// How long it takes to try to sequence a mutation.
    /// </summary>
    [DataField]
    public TimeSpan SequenceDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Damage dealt to the mob is sequencing a mutation fails.
    /// </summary>
    [DataField]
    public DamageSpecifier SequenceFailDamage = new DamageSpecifier()
    {
        DamageDict = new()
        {
            { "Cellular", 50 }
        }
    };

    /// <summary>
    /// Sound played if sequencing a mutation fails.
    /// </summary>
    [DataField]
    public SoundSpecifier? SequenceFailSound;

    /// <summary>
    /// Sound played if sequencing a mutation succeeds.
    /// </summary>
    [DataField]
    public SoundSpecifier? SequenceSound;

    #endregion

    #region Writing

    /// <summary>
    /// Sound played when writing a mutation to the inserted disk.
    /// </summary>
    [DataField]
    public SoundSpecifier? WriteSound;

    /// <summary>
    /// How long you have to wait before writing again.
    /// </summary>
    [DataField]
    public TimeSpan WriteDelay = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan NextWrite = TimeSpan.Zero;

    #endregion

    #region Combining

    /// <summary>
    /// Sound played when combining mutations.
    /// </summary>
    [DataField]
    public SoundSpecifier? CombineSound;

    /// <summary>
    /// How long the combining doafter is.
    /// </summary>
    [DataField]
    public TimeSpan CombineDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Damage dealt to the mob when combining a new mutation.
    /// </summary>
    [DataField]
    public DamageSpecifier CombineDamage = new DamageSpecifier()
    {
        DamageDict = new()
        {
            { "Cellular", 10 }
        }
    };

    #endregion

    #region Printing

    /// <summary>
    /// Items that can be printed and the delay for it.
    /// </summary>
    [DataField]
    public List<GeneticsPrint> Prints = new()
    {
        new(TimeSpan.FromSeconds(60), "GeneticsMutator"),
        new(TimeSpan.FromSeconds(15), "GeneticsActivator"),
        new(TimeSpan.FromSeconds(30), "GeneticsCleanser") // not parity, fuck chud mutadone
    };

    /// <summary>
    /// When the next item can be printed.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan NextPrint = TimeSpan.Zero;

    /// <summary>
    /// Sound played when printing an item.
    /// </summary>
    [DataField]
    public SoundSpecifier? PrintSound;

    #endregion
}

/// <summary>
/// Event raised on the console when a mutation is sequenced, before any changes are made to the round data.
/// </summary>
[ByRefEvent]
public readonly record struct MutationSequencedEvent(EntProtoId<MutationComponent> Mutation, MutationData Data);

[Serializable, NetSerializable]
public enum GeneticsConsoleUiKey : byte
{
    Key
}

/// <summary>
/// Ways which a base can be cycled in the gene puzzle.
/// </summary>
[Serializable, NetSerializable]
public enum GeneticsCycle : byte
{
    Reset,
    Next,
    Last
}

/// <summary>
/// Message to scramble the scanned mob's genome.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class GeneticsConsoleScrambleMessage : BoundUserInterfaceMessage;

/// <summary>
/// Message to set an unknown base to a certain char.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class GeneticsConsoleSetBaseMessage(uint sequence, uint index, GeneticsCycle cycle) : BoundUserInterfaceMessage
{
    public readonly uint Sequence = sequence;
    public readonly uint Index = index;
    public readonly GeneticsCycle Cycle = cycle;
}

/// <summary>
/// Message to start the sequencing process for a mutation.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class GeneticsConsoleSequenceMessage(uint index) : BoundUserInterfaceMessage
{
    public readonly uint Index = index;
}

/// <summary>
/// Message to write a given mutation to the current disk.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class GeneticsConsoleWriteMutationMessage(uint index) : BoundUserInterfaceMessage
{
    public readonly uint Index = index;
}

/// <summary>
/// Message to print an item from the current disk's mutation.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class GeneticsConsolePrintMessage(uint print) : BoundUserInterfaceMessage
{
    public readonly uint Print = print;
}

/// <summary>
/// Message to create a new combined mutation from the current disk's mutation and a selected mutation on the mob.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class GeneticsConsoleCombineMessage(uint index) : BoundUserInterfaceMessage
{
    public readonly uint Index = index;
}

/// <summary>
/// BUI state containing the target mob's sequences client state.
/// </summary>
[Serializable, NetSerializable]
public sealed class GeneticsConsoleState(List<SequenceState> sequences) : BoundUserInterfaceState
{
    public readonly List<SequenceState> Sequences = sequences;
}

[DataRecord]
public partial record struct GeneticsPrint(TimeSpan Delay, EntProtoId Proto);
