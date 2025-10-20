using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Damage;
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
public sealed partial class GeneticsConsoleComponent : Component
{
    /// <summary>
    /// The number of each stored chromosome, indexed by its integer value
    /// </summary>
    [DataField, AutoNetworkedField]
    public int[] Chromosomes = new int[4];

    /// <summary>
    /// Name of the item slot that holds a genetics disk.
    /// </summary>
    [DataField]
    public string DiskSlot = "genetics_disk";

    /// <summary>
    /// Subjects with more than this number of genetic damage can't be scanned or sequenced.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxGeneticDamage = 90;

    #region Scanning

    /// <summary>
    /// The linked medical scanner.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Scanner;

    /// <summary>
    /// The mob currently in a linked scanner.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ScannedMob;

    /// <summary>
    /// How long it takes to scan a mob's genome.
    /// </summary>
    [DataField]
    public TimeSpan ScanDelay = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Sound played after successfully scanning a mob.
    /// </summary>
    [DataField]
    public SoundSpecifier? ScanSound;

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
    /// Used to prevent scanning/sequencing at the same time.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Busy;

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
    public TimeSpan NextWrite = TimeSpan.Zero;

    #endregion

    #region Printing

    /// <summary>
    /// How long you have to wait before printing another mutator.
    /// </summary>
    [DataField]
    public TimeSpan MutatorDelay = TimeSpan.FromSeconds(60);

    /// <summary>
    /// How long you have to wait before printing another activator.
    /// </summary>
    [DataField]
    public TimeSpan ActivatorDelay = TimeSpan.FromSeconds(15);

    /// <summary>
    /// How long you have to wait before printing another mutator/activator.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextPrint = TimeSpan.Zero;

    #endregion
}

[Serializable, NetSerializable]
public enum GeneticsConsoleUiKey : byte
{
    Key
}

/// <summary>
/// Message to start the scanning process for an unscanned mob in the scanner.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class GeneticsConsoleScanMessage : BoundUserInterfaceMessage;

/// <summary>
/// Message to set an unknown base to a certain char.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class GeneticsConsoleSetBaseMessage(uint sequence, uint index, char b) : BoundUserInterfaceMessage
{
    public readonly uint Sequence = sequence;
    public readonly uint Index = index;
    public readonly char Base = b;
}

/// <summary>
/// Message to use joker to correct a base for a mutation.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class GeneticsConsoleJokerMessage(uint index) : BoundUserInterfaceMessage
{
    public readonly uint Index = index;
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
/// Message to print a mutator or activator from the current disk's mutation.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class GeneticsConsolePrintMessage(bool activator, Chromosome chromosome) : BoundUserInterfaceMessage
{
    public readonly bool Activator = activator;
    public readonly Chromosome Chromosome = chromosome;
}

/// <summary>
/// BUI state containing the target mob's sequences client state.
/// </summary>
[Serializable, NetSerializable]
public sealed class GeneticsConsoleState(List<SequenceState> sequences) : BoundUserInterfaceState
{
    public readonly List<SequenceState> Sequences = sequences;
}
