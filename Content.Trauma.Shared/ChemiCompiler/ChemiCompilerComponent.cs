// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;
using Content.Shared.Materials;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.ChemiCompiler;

/// <summary>
/// A chemistry machine programmed in ChemFuck, a Brainfuck derivative with extra instructions for moving,
/// heating and packaging reagents between its reservoirs.
/// Stores <see cref="CodeSlots"/> programs, any of which can be run against the beakers in its reservoirs.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChemiCompilerComponent : Component
{
    /// <summary>
    /// How many programs the machine can hold at once.
    /// </summary>
    public const int CodeSlots = 6;

    /// <summary>
    /// How many beakers can be loaded into the machine.
    /// These are the valid values for the source register, and the first <see cref="Reservoirs"/> target values.
    /// </summary>
    public const int Reservoirs = 10;

    /// <summary>
    /// Target register value that turns transferred reagents into pills.
    /// </summary>
    public const int TargetPills = Reservoirs + 1;

    /// <summary>
    /// Target register value that turns transferred reagents into a vial.
    /// </summary>
    public const int TargetVial = Reservoirs + 2;

    /// <summary>
    /// Target register value that throws transferred reagents away.
    /// </summary>
    public const int TargetEject = Reservoirs + 3;

    /// <summary>
    /// Target register value that fills the first entry in <see cref="PatchPrototypes"/>.
    /// Each value after it fills the next entry.
    /// </summary>
    public const int TargetPatchFirst = TargetEject + 1;

    /// <summary>
    /// How many memory cells a running program gets. Each cell holds a single byte.
    /// </summary>
    public const int MemorySize = 1024;

    /// <summary>
    /// Longest a single program can be.
    /// Programs are mostly runs of + and - so this needs to be generous, but not so generous that someone
    /// can paste a novel into the machine and make the server serialize it forever.
    /// </summary>
    [DataField]
    public int MaxProgramLength = 8192;

    /// <summary>
    /// How long a program may run before it gets forcibly halted.
    /// This is what stops infinite loops from tying the machine up forever. Generous enough that a real
    /// program with several heating steps finishes comfortably.
    /// </summary>
    [DataField]
    public TimeSpan MaxRuntime = TimeSpan.FromMinutes(10);

    /// <summary>
    /// How many instructions a program may run before it gets forcibly halted.
    /// A backstop behind <see cref="MaxRuntime"/>, which is the limit that normally bites.
    /// </summary>
    [DataField]
    public int MaxInstructions = 100000;

    /// <summary>
    /// How long a <see cref="ChemFuck.Speed.Fast"/> instruction takes.
    /// Arithmetic is how you write numbers in this language, so it has to stay cheap enough that setting a
    /// register to 100 isn't the slowest part of a program.
    /// </summary>
    [DataField]
    public TimeSpan FastDelay = TimeSpan.FromSeconds(0.02);

    /// <summary>
    /// How long a <see cref="ChemFuck.Speed.Normal"/> instruction takes.
    /// </summary>
    [DataField]
    public TimeSpan InstructionDelay = TimeSpan.FromSeconds(0.1);

    /// <summary>
    /// How long a <see cref="ChemFuck.Speed.Physical"/> instruction takes.
    /// This is the number that decides how fast the machine can actually do chemistry.
    /// </summary>
    [DataField]
    public TimeSpan PhysicalDelay = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// Most instructions that may be run in a single tick.
    /// This is not the pacing mechanism, the delays above are. It only bounds how much a machine may catch
    /// up on after the server hitches, so a stall can't dump half a program in one tick.
    /// </summary>
    [DataField]
    public int MaxInstructionsPerTick = 64;

    /// <summary>
    /// Longest a line written by <see cref="ChemFuck.Output"/> can get before the machine says it anyway.
    /// Without this a program that never writes a newline would build a string forever.
    /// </summary>
    [DataField]
    public int MaxOutputLength = 256;

    /// <summary>
    /// The saved programs, one per code slot. Empty string means the slot is empty.
    /// Not networked, these are sent to whoever has the UI open instead.
    /// </summary>
    [DataField(serverOnly: true)]
    public string[] Programs = new string[CodeSlots];

    /// <summary>
    /// Whether each program has been locked with <see cref="ChemFuck.Lock"/>.
    /// A locked program can still be overwritten, it just can't be read back out again.
    /// </summary>
    [DataField(serverOnly: true)]
    public bool[] Locked = new bool[CodeSlots];

    /// <summary>
    /// Item slot ids are this followed by the 1-based reservoir number, e.g. "reservoir1".
    /// </summary>
    [DataField]
    public string SlotPrefix = "reservoir";

    /// <summary>
    /// Most reagents a single pill from the pill generator can hold.
    /// </summary>
    [DataField]
    public FixedPoint2 PillDosage = FixedPoint2.New(20);

    /// <summary>
    /// What the pill generator spawns.
    /// </summary>
    [DataField]
    public EntProtoId PillPrototype = "Pill";

    /// <summary>
    /// What the vial generator spawns.
    /// </summary>
    [DataField]
    public EntProtoId VialPrototype = "ChemistryEmptyVial";

    /// <summary>
    /// What one vial costs. Matches its lathe recipe rather than what grinding one returns, so printing
    /// here is never cheaper than printing it properly.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<MaterialPrototype>, int> VialCost = new()
    {
        ["Glass"] = 100,
    };

    /// <summary>
    /// What the patch generators spawn, starting at <see cref="TargetPatchFirst"/>.
    /// </summary>
    [DataField]
    public List<EntProtoId> PatchPrototypes = new()
    {
        "MedicalPatchBasic",
        "MedicalPatchRapid",
        "MedicalPatchLarge",
        "MedicalPatchTherapeutic",
    };

    /// <summary>
    /// What one patch costs, the same for every kind, matching their lathe recipes.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<MaterialPrototype>, int> PatchCost = new()
    {
        ["Cloth"] = 100,
        ["Plastic"] = 100,
    };

    /// <summary>
    /// How much heat <see cref="ChemFuck.Heat"/> can push into a reservoir per second, in joules.
    /// Deliberately the same as a hotplate's <c>heatPerSecond</c> so automating your chemistry doesn't also
    /// make it faster than doing it by hand. Cooling uses the same rate in reverse.
    /// </summary>
    [DataField]
    public float HeatPerSecond = 160f;

    /// <summary>
    /// Shortest a heat instruction can take, so nudging a reservoir a fraction of a degree still costs a moment.
    /// </summary>
    [DataField]
    public TimeSpan MinHeatDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How long a <see cref="ChemFuck.Speed.Slow"/> instruction takes.
    /// The slowest thing a single instruction can cost, so a program can pause long enough for a reaction
    /// to settle before carrying on.
    /// </summary>
    [DataField]
    public TimeSpan SlowDelay = TimeSpan.FromSeconds(1);

    // The machine talks in beeps, with the buzzer saved for things going wrong so an error never sounds like
    // ordinary progress. Volumes sit around where other machines put their one-shots.
    // Note that gain is 10^(dB/10) here, not the usual 10^(dB/20), so these numbers move faster than they look:
    // -2dB is 63% gain, but -16dB is 2.5% and may as well be silence.

    /// <summary>
    /// Played when a program starts.
    /// </summary>
    [DataField]
    public SoundSpecifier StartSound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg",
        AudioParams.Default.WithVolume(-2f));

    /// <summary>
    /// Played when an instruction fails, usually a missing beaker.
    /// The only sound that isn't a beep, because a failure should never be mistaken for progress.
    /// </summary>
    [DataField]
    public SoundSpecifier FailSound = new SoundPathSpecifier("/Audio/Machines/buzz-two.ogg",
        AudioParams.Default.WithVolume(-2f));

    /// <summary>
    /// Played whenever reagents leave a reservoir, whether they land in another one or in a pill or vial.
    /// Fires on every transfer, so a program's rhythm can be heard.
    /// </summary>
    [DataField]
    public SoundSpecifier TransferSound = new SoundPathSpecifier("/Audio/Machines/beep.ogg",
        AudioParams.Default.WithVolume(-4f));

    /// <summary>
    /// Played at the start and end of heating a reservoir.
    /// Pitched below <see cref="IdleSound"/> so the two are told apart by ear alone.
    /// </summary>
    [DataField]
    public SoundSpecifier HeatSound = new SoundPathSpecifier("/Audio/Machines/beep.ogg",
        AudioParams.Default.WithVolume(-3f).WithPitchScale(0.85f));

    /// <summary>
    /// Played when a program finishes and the machine goes idle.
    /// </summary>
    [DataField]
    public SoundSpecifier IdleSound = new SoundPathSpecifier("/Audio/Machines/beep.ogg",
        AudioParams.Default.WithVolume(-2f).WithPitchScale(1.3f));

    /// <summary>
    /// Shortest gap between two fail buzzes.
    /// A program failing every instruction should sound annoyed, not continuous.
    /// </summary>
    [DataField]
    public TimeSpan SoundCooldown = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets the item slot id for a 1-based reservoir number.
    /// </summary>
    public string SlotId(int reservoir)
        => $"{SlotPrefix}{reservoir}";

    /// <summary>
    /// Gets the patch a target register value prints, if it points at a patch generator at all.
    /// </summary>
    public bool TryGetPatch(int target, out EntProtoId patch)
    {
        patch = default;

        var index = target - TargetPatchFirst;
        if (index < 0 || index >= PatchPrototypes.Count)
            return false;

        patch = PatchPrototypes[index];
        return true;
    }

    /// <summary>
    /// How long an instruction of a given speed ties the machine up for.
    /// </summary>
    public TimeSpan DelayFor(ChemFuck.Speed speed)
        => speed switch
        {
            ChemFuck.Speed.Fast => FastDelay,
            ChemFuck.Speed.Physical => PhysicalDelay,
            ChemFuck.Speed.Slow => SlowDelay,
            _ => InstructionDelay,
        };
}

[Serializable, NetSerializable]
public enum ChemiCompilerUiKey : byte
{
    Key
}

/// <summary>
/// State for the ChemiCompiler BUI.
/// Programs and memory are far too big to network to everyone, so this only goes to whoever is using it.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChemiCompilerState(
    string?[] programs,
    bool[] filled,
    bool[] reservoirs,
    bool running,
    int source,
    int target,
    int amount
) : BoundUserInterfaceState
{
    /// <summary>
    /// Code for each slot, null when the slot is empty or has been locked.
    /// </summary>
    public readonly string?[] Programs = programs;

    /// <summary>
    /// Whether each slot holds a program, including locked ones that can't be read back.
    /// </summary>
    public readonly bool[] Filled = filled;

    /// <summary>
    /// Whether each reservoir holds a beaker.
    /// </summary>
    public readonly bool[] Reservoirs = reservoirs;

    /// <summary>
    /// Whether a program is running right now, in which case the machine ignores everything else.
    /// </summary>
    public readonly bool Running = running;

    /// <summary>
    /// Registers as they were when the last program halted.
    /// </summary>
    public readonly int Source = source;
    public readonly int Target = target;
    public readonly int Amount = amount;
}

/// <summary>
/// Save the code in the editor to a slot, overwriting whatever was there.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChemiCompilerSaveMessage(int slot, string code) : BoundUserInterfaceMessage
{
    public readonly int Slot = slot;
    public readonly string Code = code;
}

/// <summary>
/// Start running the program in a slot.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChemiCompilerRunMessage(int slot) : BoundUserInterfaceMessage
{
    public readonly int Slot = slot;
}

/// <summary>
/// Insert a held beaker into a 1-based reservoir, or take it back out if the reservoir is full.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChemiCompilerReservoirMessage(int reservoir) : BoundUserInterfaceMessage
{
    public readonly int Reservoir = reservoir;
}

/// <summary>
/// Stop the running program early.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChemiCompilerHaltMessage : BoundUserInterfaceMessage;
