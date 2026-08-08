// SPDX-License-Identifier: AGPL-3.0-or-later

// The ChemiCompiler and its ChemFuck instruction set are a reimplementation of the machine of the same name from
// Goonstation, written from the behaviour documented at https://wiki.ss13.co/ChemiCompiler.
// No Goonstation code was used, this is an original implementation of the described machine.

using Content.Server.Chat.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Materials;
using Content.Shared.Power.EntitySystems;
using Content.Trauma.Shared.ChemiCompiler;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Content.Trauma.Server.ChemiCompiler;

/// <summary>
/// Runs ChemFuck programs on a <see cref="ChemiCompilerComponent"/>, a few instructions per tick.
/// Server only, because programs and their memory are far too big to network.
/// </summary>
public sealed partial class ChemiCompilerRunnerSystem : EntitySystem
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private ChemiCompilerSystem _compiler = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedMaterialStorageSystem _material = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;

    [Dependency] private EntityQuery<ChemiCompilerComponent> _query = default!;
    [Dependency] private EntityQuery<MaterialStorageComponent> _storageQuery = default!;

    /// <summary>
    /// Reusable buffer so speaking doesn't allocate a new builder each time.
    /// </summary>
    private StringBuilder _builder = new();

    /// <summary>
    /// What a single instruction did, and so what the machine should do next.
    /// </summary>
    private enum Step : byte
    {
        /// <summary>
        /// Carry straight on to the next instruction.
        /// </summary>
        Next,

        /// <summary>
        /// The instruction couldn't be carried out. Beep about it and carry on anyway, like the real thing does.
        /// </summary>
        Failed,

        /// <summary>
        /// The instruction scheduled a long operation of its own and set its own deadline for it.
        /// Only heating does this; everything else is paced by its speed tier.
        /// </summary>
        Wait,
    }

    /// <summary>
    /// Says whatever the program had written but not yet finished a line with.
    /// A program can stop five different ways, and two of them live in the shared system where the chat
    /// system isn't available. Hooking the shutdown catches every route with one subscription.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnActiveShutdown(Entity<ActiveChemiCompilerComponent> ent, ref ComponentShutdown args)
    {
        // the component also shuts down when the machine itself is being deleted, and a dying entity
        // has no business talking
        if (TerminatingOrDeleted(ent.Owner))
            return;

        if (!_query.TryComp(ent, out var comp))
            return;

        Speak((ent.Owner, comp), ent.Comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // active component first, so idle machines are never touched
        var query = EntityQueryEnumerator<ActiveChemiCompilerComponent, ChemiCompilerComponent>();
        while (query.MoveNext(out var uid, out var active, out var comp))
        {
            var ent = (uid, comp);

            if (!_power.IsPowered(uid))
            {
                _compiler.Halt(ent, "chemicompiler-halted-power");
                continue;
            }

            // heating happens while the program is stopped waiting for it, so this runs before the wait check
            if (active.PendingHeat != null)
                UpdateHeating(ent, active);

            if (_timing.CurTime < active.NextStep)
                continue;

            // Accumulating NextStep is only meant to carry the sub-tick remainder forward. If it has fallen
            // further behind than that, something stalled, and without this the catch-up below would run a
            // burst of effectively free instructions.
            var floor = _timing.CurTime - _timing.TickPeriod;
            if (active.NextStep < floor)
                active.NextStep = floor;

            // Instruction delays are finer than a tick, so a tick can owe more than one instruction.
            // Bounded so catching up stays steady instead of dumping half a program at once.
            for (var i = 0; i < comp.MaxInstructionsPerTick; i++)
            {
                if (!Run(ent, active) || _timing.CurTime < active.NextStep)
                    break;
            }
        }
    }

    /// <summary>
    /// Runs a single instruction and works out when the next one may run.
    /// Every instruction costs time, so the machine visibly works through a program rather than finishing
    /// the whole thing in the tick you press the button.
    /// </summary>
    /// <returns>False if the program halted.</returns>
    private bool Run(Entity<ChemiCompilerComponent> ent, ActiveChemiCompilerComponent active)
    {
        var comp = ent.Comp;

        // whitespace and comments the player left in for their own sake are free to skip past
        while (active.Instruction >= 0 &&
               active.Instruction < active.Program.Length &&
               !ChemFuck.IsInstruction(active.Program[active.Instruction]))
        {
            active.Instruction++;
        }

        if (active.Instruction < 0 || active.Instruction >= active.Program.Length)
        {
            // ran off the end, the program is done
            _compiler.Halt(ent);
            return false;
        }

        if (_timing.CurTime - active.Started > comp.MaxRuntime)
        {
            Log.Debug($"ChemiCompiler {ToPrettyString(ent)} ran out of time on program {active.Slot + 1}");
            _compiler.Halt(ent, "chemicompiler-halted-timeout");
            return false;
        }

        if (++active.Executed > comp.MaxInstructions)
        {
            Log.Debug($"ChemiCompiler {ToPrettyString(ent)} hit its instruction limit running program {active.Slot + 1}");
            _compiler.Halt(ent, "chemicompiler-halted-limit");
            return false;
        }

        var instruction = active.Program[active.Instruction];
        var step = Execute(ent, active, instruction);
        active.Instruction++;

        switch (step)
        {
            case Step.Failed:
                if (_timing.CurTime >= active.NextFailSound)
                {
                    _audio.PlayPvs(comp.FailSound, ent);
                    active.NextFailSound = _timing.CurTime + comp.SoundCooldown;
                }
                break;

            case Step.Wait:
                // heating works out its own, much longer, deadline that must not be trampled
                return true;
        }

        // Advance from the deadline just met rather than from now. Delays finer than a tick would otherwise
        // round up to a whole tick each, making a 0.02s instruction really cost 0.033s.
        active.NextStep += comp.DelayFor(ChemFuck.SpeedOf(instruction));
        return true;
    }

    /// <summary>
    /// Carries out a single instruction.
    /// </summary>
    private Step Execute(Entity<ChemiCompilerComponent> ent, ActiveChemiCompilerComponent active, char instruction)
    {
        switch (instruction)
        {
            case ChemFuck.PointerRight:
                // the pointer wraps so a runaway program can't walk off the end of memory
                active.Pointer = (active.Pointer + 1) % ChemiCompilerComponent.MemorySize;
                return Step.Next;

            case ChemFuck.PointerLeft:
                active.Pointer = (active.Pointer + ChemiCompilerComponent.MemorySize - 1) % ChemiCompilerComponent.MemorySize;
                return Step.Next;

            case ChemFuck.Increment:
                active.Memory[active.Pointer]++;
                return Step.Next;

            case ChemFuck.Decrement:
                active.Memory[active.Pointer]--;
                return Step.Next;

            case ChemFuck.LoopStart:
                // zero means skip the body entirely
                if (active.Memory[active.Pointer] == 0)
                    active.Instruction = active.JumpTable[active.Instruction];
                return Step.Next;

            case ChemFuck.LoopEnd:
                // nonzero means go round again
                if (active.Memory[active.Pointer] != 0)
                    active.Instruction = active.JumpTable[active.Instruction];
                return Step.Next;

            case ChemFuck.StoreSource:
                active.Source = active.Memory[active.Pointer];
                return Step.Next;

            case ChemFuck.LoadSource:
                active.Memory[active.Pointer] = (byte) active.Source;
                return Step.Next;

            case ChemFuck.StoreTarget:
                active.Target = active.Memory[active.Pointer];
                return Step.Next;

            case ChemFuck.LoadTarget:
                active.Memory[active.Pointer] = (byte) active.Target;
                return Step.Next;

            case ChemFuck.StoreAmount:
                active.Amount = active.Memory[active.Pointer];
                return Step.Next;

            case ChemFuck.LoadAmount:
                active.Memory[active.Pointer] = (byte) active.Amount;
                return Step.Next;

            case ChemFuck.Measure:
                return Measure(ent, active);

            case ChemFuck.Heat:
                return Heat(ent, active);

            case ChemFuck.Transfer:
                return Transfer(ent, active);

            case ChemFuck.Isolate:
                return Isolate(ent, active);

            case ChemFuck.Output:
                Write(ent, active, active.Memory[active.Pointer]);
                return Step.Next;

            case ChemFuck.Lock:
                ent.Comp.Locked[active.Slot] = true;
                return Step.Next;

            case ChemFuck.Nop:
                // doing nothing is the whole instruction, the slow tier is what makes it take a moment
                return Step.Next;

            default:
                return Step.Next;
        }
    }

    /// <summary>
    /// Adds a character to the line the program is building up.
    /// A newline sends the line, and so does filling the buffer, so a program that never writes one still
    /// gets its text out instead of silently losing characters.
    /// </summary>
    private void Write(Entity<ChemiCompilerComponent> ent, ActiveChemiCompilerComponent active, byte value)
    {
        if (value == '\n')
        {
            Speak(ent, active);
            return;
        }

        active.Output.Append((char) value);

        if (active.Output.Length >= ent.Comp.MaxOutputLength)
            Speak(ent, active);
    }

    /// <summary>
    /// Says the line the program has built up, then clears it.
    /// </summary>
    private void Speak(Entity<ChemiCompilerComponent> ent, ActiveChemiCompilerComponent active)
    {
        _builder.Clear();
        _builder.EnsureCapacity(active.Output.Length);

        // a program can write any byte it likes, and most of the low ones are control codes that would
        // come out as rubbish in a speech bubble
        for (var i = 0; i < active.Output.Length; i++)
        {
            var c = active.Output[i];
            if (!char.IsControl(c))
                _builder.Append(c);
        }

        // cleared before the early return, or a line of nothing but control codes would stick forever
        active.Output.Clear();

        if (_builder.Length == 0)
            return;

        var line = FormattedMessage.EscapeText(_builder.ToString());

        // hidden from chat so a program in a loop can't bury the round's chat log
        _chat.TrySendInGameICMessage(ent, line, InGameICChatType.Speak, hideChat: true);
    }

    /// <summary>
    /// Reads how full the source reservoir is into the amount register.
    /// </summary>
    private Step Measure(Entity<ChemiCompilerComponent> ent, ActiveChemiCompilerComponent active)
    {
        if (!TryGetReservoir(ent, active.Source, out _, out var solution))
            return Step.Failed;

        active.Amount = solution.Volume.Int();
        return Step.Next;
    }

    /// <summary>
    /// Starts bringing the source reservoir to (273 - target) + amount Kelvin.
    /// How long that takes comes from the same energy budget a hotplate works to, so a program can't heat
    /// anything faster than a chemist standing over a hotplate could.
    /// </summary>
    private Step Heat(Entity<ChemiCompilerComponent> ent, ActiveChemiCompilerComponent active)
    {
        if (!TryGetReservoir(ent, active.Source, out _, out var solution))
            return Step.Failed;

        var target = 273f - active.Target + active.Amount;

        active.PendingHeat = target;
        active.PendingHeatSource = active.Source;
        active.HeatStartTemperature = solution.Temperature;
        active.HeatStart = _timing.CurTime;
        active.NextStep = _timing.CurTime + HeatDuration(ent, solution, target);

        _audio.PlayPvs(ent.Comp.HeatSound, ent);
        return Step.Wait;
    }

    /// <summary>
    /// How long it takes to move a solution to a temperature, from the energy needed and how fast the machine
    /// can supply it. This is the hotplate's model: energy = heat capacity * change in temperature.
    /// </summary>
    private TimeSpan HeatDuration(Entity<ChemiCompilerComponent> ent, Solution solution, float target)
    {
        var energy = solution.GetHeatCapacity(ProtoMan) * MathF.Abs(target - solution.Temperature);
        var seconds = energy / ent.Comp.HeatPerSecond;

        var delay = TimeSpan.FromSeconds(seconds);
        return delay < ent.Comp.MinHeatDelay ? ent.Comp.MinHeatDelay : delay;
    }

    /// <summary>
    /// Walks a reservoir's temperature towards its target while the program waits.
    /// Doing it gradually rather than all at once means reactions with a temperature requirement fire on the
    /// way up, exactly like leaving a beaker sitting on a hotplate.
    /// </summary>
    private void UpdateHeating(Entity<ChemiCompilerComponent> ent, ActiveChemiCompilerComponent active)
    {
        if (active.PendingHeat is not { } target ||
            !TryGetReservoir(ent, active.PendingHeatSource, out var soln, out _))
        {
            active.PendingHeat = null; // beaker went missing, give up on it
            return;
        }

        if (_timing.CurTime >= active.NextStep)
        {
            _solution.SetTemperature(soln.Value, target);
            _audio.PlayPvs(ent.Comp.HeatSound, ent);
            active.PendingHeat = null;
            return;
        }

        var total = (active.NextStep - active.HeatStart).TotalSeconds;
        var elapsed = (_timing.CurTime - active.HeatStart).TotalSeconds;
        var progress = total <= 0 ? 1f : (float) (elapsed / total);

        _solution.SetTemperature(soln.Value, MathHelper.Lerp(active.HeatStartTemperature, target, progress));
    }

    /// <summary>
    /// Moves the amount register's worth of reagents out of the source reservoir and into whatever the target is.
    /// </summary>
    private Step Transfer(Entity<ChemiCompilerComponent> ent, ActiveChemiCompilerComponent active)
    {
        if (active.Amount <= 0)
            return Step.Next; // moving nothing isn't a failure, it just does nothing

        if (!TryGetReservoir(ent, active.Source, out var soln, out _))
            return Step.Failed;

        var split = _solution.SplitSolution(soln.Value, FixedPoint2.New(active.Amount));
        if (split.Volume <= FixedPoint2.Zero)
            return Step.Next; // source was empty

        return Deliver(ent, active, soln.Value, split);
    }

    /// <summary>
    /// Same as <see cref="Transfer"/>, but only takes the one reagent the data pointer is naming.
    /// The pointer holds a 1-based index into the source's contents.
    /// </summary>
    private Step Isolate(Entity<ChemiCompilerComponent> ent, ActiveChemiCompilerComponent active)
    {
        if (active.Amount <= 0)
            return Step.Next;

        if (!TryGetReservoir(ent, active.Source, out var soln, out var solution))
            return Step.Failed;

        var index = active.Memory[active.Pointer] - 1;
        if (index < 0 || index >= solution.Contents.Count)
            return Step.Failed; // no such reagent in there

        var reagent = solution.Contents[index].Reagent.Prototype;
        var split = solution.SplitSolutionWithOnly(FixedPoint2.New(active.Amount), reagent);
        _solution.UpdateChemicals(soln.Value);

        if (split.Volume <= FixedPoint2.Zero)
            return Step.Next;

        return Deliver(ent, active, soln.Value, split);
    }

    /// <summary>
    /// Puts reagents that have already been taken out of a reservoir wherever the target register points.
    /// Anything that doesn't fit goes back where it came from, so reagents are never quietly lost.
    /// </summary>
    private Step Deliver(
        Entity<ChemiCompilerComponent> ent,
        ActiveChemiCompilerComponent active,
        Entity<SolutionComponent> source,
        Solution split)
    {
        var comp = ent.Comp;

        switch (active.Target)
        {
            case ChemiCompilerComponent.TargetPills:
                MakePills(ent, split);
                break;

            case ChemiCompilerComponent.TargetVial:
                if (!MakeVial(ent, split))
                {
                    Refund(source, split);
                    return Step.Failed;
                }
                break;

            case ChemiCompilerComponent.TargetEject:
                // straight down the drain, which is the whole point of the ejection port
                split.RemoveAllSolution();
                break;

            default:
                // patch targets sit above the reservoirs, and there are too many of them to be cases
                if (comp.TryGetPatch(active.Target, out var patch))
                {
                    if (!MakePatch(ent, patch, split))
                    {
                        Refund(source, split);
                        return Step.Failed;
                    }

                    break;
                }

                if (active.Target < 1 || active.Target > ChemiCompilerComponent.Reservoirs ||
                    !TryGetReservoir(ent, active.Target, out var target, out _))
                {
                    Refund(source, split);
                    return Step.Failed;
                }

                _solution.TryTransferSolution(target.Value, split, split.Volume);
                break;
        }

        // every transfer beeps. physical instructions are half a second apart, so this stays a rhythm rather
        // than a stream, and it is the only way to hear that the program is doing chemistry at all
        _audio.PlayPvs(comp.TransferSound, ent);

        // whatever wouldn't fit goes back in the source beaker
        Refund(source, split);
        return Step.Next;
    }

    /// <summary>
    /// Turns reagents into as many pills as they'll fill, capped at the machine's dosage per pill.
    /// </summary>
    private void MakePills(Entity<ChemiCompilerComponent> ent, Solution split)
    {
        var comp = ent.Comp;
        var coords = Transform(ent).Coordinates;

        while (split.Volume > FixedPoint2.Zero)
        {
            var dosage = FixedPoint2.Min(comp.PillDosage, split.Volume);
            var pill = Spawn(comp.PillPrototype, coords);

            _solution.EnsureSolution(pill, SharedChemMaster.PillSolutionName, out var solution);
            solution.Comp.Solution.MaxVolume = dosage;
            _solution.TryAddSolution(solution, split.SplitSolution(dosage));

            EnsureComp<PillComponent>(pill);

            _adminLog.Add(LogType.ChemiCompiler,
                LogImpact.Low,
                $"ChemiCompiler {ent.Owner:machine} printed {pill:pill} {SharedSolutionContainerSystem.ToPrettyString(solution.Comp.Solution)}");
        }
    }

    /// <summary>
    /// Fills a fresh vial with as much as it will hold, if there's glass in the machine to make one out of.
    /// </summary>
    /// <returns>False if the machine couldn't afford the glass.</returns>
    private bool MakeVial(Entity<ChemiCompilerComponent> ent, Solution split)
    {
        var comp = ent.Comp;

        if (!TrySpend(ent, comp.VialCost))
            return false;

        var vial = Spawn(comp.VialPrototype, Transform(ent).Coordinates);
        if (!_solution.TryGetFitsInDispenser(vial, out var soln, out _))
            return true;

        _solution.TryTransferSolution(soln.Value, split, split.Volume);

        _adminLog.Add(LogType.ChemiCompiler,
            LogImpact.Low,
            $"ChemiCompiler {ent.Owner:machine} filled {vial:vial} {SharedSolutionContainerSystem.ToPrettyString(soln.Value.Comp.Solution)}");
        return true;
    }

    /// <summary>
    /// Fills a fresh patch with as much as it will hold, if the machine has the cloth and plastic for one.
    /// </summary>
    /// <returns>False if the machine couldn't afford the materials.</returns>
    private bool MakePatch(Entity<ChemiCompilerComponent> ent, EntProtoId proto, Solution split)
    {
        if (!TrySpend(ent, ent.Comp.PatchCost))
            return false;

        var patch = Spawn(proto, Transform(ent).Coordinates);

        // patches don't fit in a dispenser, but they hold their dose in the same solution a bottle does.
        // its volume is set per patch prototype, which is the only thing telling a large patch from a basic one
        if (!_solution.TryGetSolution(patch, SharedChemMaster.BottleSolutionName, out var soln, out _))
            return true;

        _solution.TryTransferSolution(soln.Value, split, split.Volume);

        _adminLog.Add(LogType.ChemiCompiler,
            LogImpact.Low,
            $"ChemiCompiler {ent.Owner:machine} filled {patch:patch} {SharedSolutionContainerSystem.ToPrettyString(soln.Value.Comp.Solution)}");
        return true;
    }

    /// <summary>
    /// Takes the cost of one printed item out of the machine's material storage, all of it or none.
    /// </summary>
    /// <returns>False if anything was missing, in which case nothing was spent.</returns>
    private bool TrySpend(Entity<ChemiCompilerComponent> ent, Dictionary<ProtoId<MaterialPrototype>, int> cost)
    {
        if (cost.Count == 0)
            return true;

        // no material storage at all means nothing can be afforded, rather than everything being free
        if (!_storageQuery.TryComp(ent, out var storage))
            return false;

        var spend = cost.ToDictionary(pair => pair.Key, pair => -pair.Value);
        return _material.TryChangeMaterialAmount((ent.Owner, storage), spend);
    }

    /// <summary>
    /// Puts leftovers back into the reservoir they were taken from.
    /// </summary>
    private void Refund(Entity<SolutionComponent> source, Solution split)
    {
        if (split.Volume > FixedPoint2.Zero)
            _solution.TryTransferSolution(source, split, split.Volume);
    }

    /// <summary>
    /// Gets the solution in a 1-based reservoir, if there's a beaker in it at all.
    /// </summary>
    private bool TryGetReservoir(
        Entity<ChemiCompilerComponent> ent,
        int reservoir,
        [NotNullWhen(true)] out Entity<SolutionComponent>? soln,
        [NotNullWhen(true)] out Solution? solution)
    {
        soln = null;
        solution = null;

        if (reservoir < 1 || reservoir > ChemiCompilerComponent.Reservoirs)
            return false;

        if (_slots.GetItemOrNull(ent, ent.Comp.SlotId(reservoir)) is not { } beaker)
            return false;

        return _solution.TryGetFitsInDispenser(beaker, out soln, out solution);
    }
}
