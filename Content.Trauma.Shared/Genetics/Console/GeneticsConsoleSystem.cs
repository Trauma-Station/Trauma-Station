using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.UserInterface;
using Content.Trauma.Common.Medical;
using Content.Trauma.Shared.Genetics.Mutations;
using Content.Trauma.Shared.Genetics.Tools;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using System.Text;

namespace Content.Trauma.Shared.Genetics.Console;

public sealed class GeneticsConsoleSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly GeneticsDiskSystem _disk = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly MutatorSystem _mutator = default!;
    [Dependency] private readonly ScannedGenomeSystem _genome = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private StringBuilder _builder = new();
    private List<SequenceState> _sequences = new();

    private EntityQuery<GeneticsConsoleComponent> _query;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<GeneticsConsoleComponent>();

        SubscribeLocalEvent<GeneticsConsoleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GeneticsConsoleComponent, ScannerConnectedEvent>(OnScannerConnected);
        SubscribeLocalEvent<GeneticsConsoleComponent, ScannerDisconnectedEvent>(OnScannerDisconnected);
        SubscribeLocalEvent<GeneticsConsoleComponent, ScannerInsertedEvent>(OnScannerInserted);
        SubscribeLocalEvent<GeneticsConsoleComponent, ScannerEjectedEvent>(OnScannerEjected);
        SubscribeLocalEvent<GeneticsConsoleComponent, ScanDoAfterEvent>(OnScanDoAfter);
        SubscribeLocalEvent<GeneticsConsoleComponent, DoAfterAttemptEvent<ScanDoAfterEvent>>(OnScanCheck);
        SubscribeLocalEvent<GeneticsConsoleComponent, SequenceDoAfterEvent>(OnSequenceDoAfter);
        SubscribeLocalEvent<GeneticsConsoleComponent, DoAfterAttemptEvent<SequenceDoAfterEvent>>(OnSequenceCheck);
        SubscribeLocalEvent<GeneticsConsoleComponent, CombineDoAfterEvent>(OnCombineDoAfter);
        SubscribeLocalEvent<GeneticsConsoleComponent, DoAfterAttemptEvent<CombineDoAfterEvent>>(OnCombineCheck);
        SubscribeLocalEvent<GeneticsConsoleComponent, AfterActivatableUIOpenEvent>(OnUIOpened);

        Subs.BuiEvents<GeneticsConsoleComponent>(GeneticsConsoleUiKey.Key, subs =>
        {
            subs.Event<GeneticsConsoleScanMessage>(OnScan);
            subs.Event<GeneticsConsoleScrambleMessage>(OnScramble);
            subs.Event<GeneticsConsoleSetBaseMessage>(OnSetBase);
            subs.Event<GeneticsConsoleJokerMessage>(OnJoker);
            subs.Event<GeneticsConsoleSequenceMessage>(OnSequence);
            subs.Event<GeneticsConsoleWriteMutationMessage>(OnWriteMutation);
            subs.Event<GeneticsConsoleCombineMessage>(OnCombine);
            subs.Event<GeneticsConsolePrintMessage>(OnPrint);
        });
    }

    private void OnMapInit(Entity<GeneticsConsoleComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextScramble = _timing.CurTime + ent.Comp.ScrambleCooldown;
        DirtyField(ent, nameof(GeneticsConsoleComponent.NextScramble));
    }

    private void OnScannerConnected(Entity<GeneticsConsoleComponent> ent, ref ScannerConnectedEvent args)
    {
        ent.Comp.Scanner = args.Scanner;
        DirtyField(ent, nameof(GeneticsConsoleComponent.Scanner));
        UpdateUI(ent);
    }

    private void OnScannerDisconnected(Entity<GeneticsConsoleComponent> ent, ref ScannerDisconnectedEvent args)
    {
        ent.Comp.Scanner = null;
        DirtyField(ent, nameof(GeneticsConsoleComponent.Scanner));
        UpdateUI(ent);
    }

    private void OnScannerInserted(Entity<GeneticsConsoleComponent> ent, ref ScannerInsertedEvent args)
    {
        ent.Comp.ScannedMob = args.Target;
        DirtyField(ent, nameof(GeneticsConsoleComponent.ScannedMob));
        UpdateUI(ent);
    }

    private void OnScannerEjected(Entity<GeneticsConsoleComponent> ent, ref ScannerEjectedEvent args)
    {
        ent.Comp.ScannedMob = null;
        DirtyField(ent, nameof(GeneticsConsoleComponent.ScannedMob));
        UpdateUI(ent);
    }

    private void OnScan(Entity<GeneticsConsoleComponent> ent, ref GeneticsConsoleScanMessage args)
    {
        if (ent.Comp.ScannedMob is not {} mob)
            return;

        if (!CanScan(ent, mob))
            return;

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            ent,
            ent.Comp.ScanDelay,
            new ScanDoAfterEvent(GetNetEntity(mob)),
            eventTarget: ent,
            target: mob);
        doAfterArgs.AttemptFrequency = AttemptFrequency.EveryTick;
        SetBusy(ent, _doAfter.TryStartDoAfter(doAfterArgs));

        Speak(ent, "scanning");
    }

    private void OnScanDoAfter(Entity<GeneticsConsoleComponent> ent, ref ScanDoAfterEvent args)
    {
        SetBusy(ent, false);
        if (args.Cancelled)
        {
            Speak(ent, "scan-failed");
            return;
        }

        args.Handled = true;
        var mob = GetEntity(args.Mob);
        if (!CanScan(ent, mob))
        {
            Speak(ent, "scan-failed");
            return;
        }

        var damage = _mutation.GetGeneticDamage(mob) ?? 0;
        if (damage > ent.Comp.MaxGeneticDamage)
        {
            Speak(ent, "genetic-damage");
            return;
        }

        _adminLog.Add(LogType.Genetics, LogImpact.Low, $"{ToPrettyString(mob)} was scanned by {ToPrettyString(args.User)} with console {ToPrettyString(ent)}");
        _audio.PlayPredicted(ent.Comp.ScanSound, ent, args.User);

        Speak(ent, "scanned");
        if (_net.IsServer)
            _genome.ScanGenome(mob);
        UpdateUI(ent);
    }

    private void OnScanCheck(Entity<GeneticsConsoleComponent> ent, ref DoAfterAttemptEvent<ScanDoAfterEvent> args)
    {
        var mob = GetEntity(args.Event.Mob);
        if (!CanKeepWorkingOn(ent, mob))
            args.Cancel();
    }

    private void OnScramble(Entity<GeneticsConsoleComponent> ent, ref GeneticsConsoleScrambleMessage args)
    {
        if (ent.Comp.ScannedMob is not {} mob ||
            !CanWorkOn(ent, mob)
            || !_genome.IsScanned(mob) || // can't scramble unscanned mobs
            _mutation.GetMutatable(mob) is not {} mutatable)
            return;

        var now = _timing.CurTime;
        if (now < ent.Comp.NextScramble)
            return;

        _adminLog.Add(LogType.Genetics, LogImpact.High, $"Scrambled genome of {ToPrettyString(mob)} by {ToPrettyString(args.Actor)} using console {ToPrettyString(ent)}");

        _damage.ChangeDamage(mob, ent.Comp.ScrambleDamage);

        ent.Comp.NextScramble = now + ent.Comp.ScrambleCooldown;
        DirtyField(ent, nameof(GeneticsConsoleComponent.NextScramble));

        _mutation.Scramble(mutatable);
        RemComp<ScannedGenomeComponent>(mob);
        UpdateUI(ent);
    }

    private void OnSetBase(Entity<GeneticsConsoleComponent> ent, ref GeneticsConsoleSetBaseMessage args)
    {
        if (ent.Comp.ScannedMob is not {} mob || !CanWorkOn(ent, mob))
            return;

        if (_genome.GetSequence(mob, args.Sequence) is not {} sequence)
            return;

        // chud language can't just set a char directly
        _builder.Clear();
        _builder.Append(sequence.Bases);
        var i = (int) args.Index;
        _builder[i] = CycleBase(_builder[i], args.Cycle);
        sequence.Bases = _builder.ToString();
        UpdateUI(ent);
    }

    private void OnJoker(Entity<GeneticsConsoleComponent> ent, ref GeneticsConsoleJokerMessage args)
    {
        if (ent.Comp.ScannedMob is not {} mob)
            return;

        if (!CanWorkOn(ent, mob))
            return;

        if (_genome.GetSequence(mob, args.Index) is not {} sequence)
            return;

        // TODO
    }

    private void OnSequence(Entity<GeneticsConsoleComponent> ent, ref GeneticsConsoleSequenceMessage args)
    {
        if (ent.Comp.ScannedMob is not {} mob)
            return;

        if (!CanWorkOn(ent, mob) || _genome.GetSequence(mob, args.Index) is not {} sequence)
            return;

        if (_mutation.GetRoundData(sequence.Mutation)?.Discovered == true)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            ent,
            ent.Comp.SequenceDelay,
            new SequenceDoAfterEvent(GetNetEntity(mob), args.Index),
            eventTarget: ent,
            target: mob);
        doAfterArgs.AttemptFrequency = AttemptFrequency.EveryTick;
        SetBusy(ent, _doAfter.TryStartDoAfter(doAfterArgs));
        Speak(ent, "sequencing");
    }

    private void OnSequenceDoAfter(Entity<GeneticsConsoleComponent> ent, ref SequenceDoAfterEvent args)
    {
        SetBusy(ent, false);
        if (args.Cancelled)
        {
            Speak(ent, "sequence-failed");
            return;
        }

        args.Handled = true;
        var mob = GetEntity(args.Mob);
        if (!CanWorkOn(ent, mob))
        {
            Speak(ent, "sequence-failed");
            return;
        }

        var damage = _mutation.GetGeneticDamage(mob) ?? 0;
        if (damage > ent.Comp.MaxGeneticDamage)
        {
            Speak(ent, "genetic-damage");
            return;
        }

        if (_net.IsClient)
            return;

        Speak(ent, SequenceMutation(ent, mob, args.Index)
            ? "sequenced"
            : "sequence-failed");
    }

    private void OnSequenceCheck(Entity<GeneticsConsoleComponent> ent, ref DoAfterAttemptEvent<SequenceDoAfterEvent> args)
    {
        var mob = GetEntity(args.Event.Mob);
        if (!CanKeepWorkingOn(ent, mob))
            args.Cancel();
    }

    private void OnWriteMutation(Entity<GeneticsConsoleComponent> ent, ref GeneticsConsoleWriteMutationMessage args)
    {
        // check delay
        var now = _timing.CurTime;
        if (now < ent.Comp.NextWrite)
            return;

        ent.Comp.NextWrite = now + ent.Comp.WriteDelay;

        if (ent.Comp.ScannedMob is not {} mob || _genome.GetSequence(mob, args.Index) is not {} sequence)
            return;

        var mutation = sequence.Mutation;
        if (_mutation.GetRoundData(mutation)?.Discovered != true)
            return;

        if (_disk.GetDisk(ent.Owner) is not {} disk || disk.Comp.Mutation == mutation)
            return;

        _adminLog.Add(LogType.Genetics, LogImpact.Low, $"{mutation} from {ToPrettyString(mob)} was written to {ToPrettyString(disk)} by {ToPrettyString(args.Actor)} using console {ToPrettyString(ent)}");
        _audio.PlayPvs(ent.Comp.WriteSound, ent);
        _disk.SetMutation(disk, mutation);
    }

    private void OnCombine(Entity<GeneticsConsoleComponent> ent, ref GeneticsConsoleCombineMessage args)
    {
        if (ent.Comp.ScannedMob is not {} mob)
            return;

        if (!CanWorkOn(ent, mob) || _genome.GetSequence(mob, args.Index) is not {} sequence)
            return;

        if (_disk.GetDisk(ent.Owner)?.Comp.Mutation == null)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            ent,
            ent.Comp.SequenceDelay,
            new CombineDoAfterEvent(GetNetEntity(mob), args.Index),
            eventTarget: ent,
            target: mob);
        doAfterArgs.AttemptFrequency = AttemptFrequency.EveryTick;
        SetBusy(ent, _doAfter.TryStartDoAfter(doAfterArgs));
        Speak(ent, "combining");
    }

    private void OnCombineDoAfter(Entity<GeneticsConsoleComponent> ent, ref CombineDoAfterEvent args)
    {
        SetBusy(ent, false);
        if (args.Cancelled)
        {
            Speak(ent, "combine-failed");
            return;
        }

        args.Handled = true;
        var mob = GetEntity(args.Mob);
        if (!CanWorkOn(ent, mob))
        {
            Speak(ent, "combine-failed");
            return;
        }

        var damage = _mutation.GetGeneticDamage(mob) ?? 0;
        if (damage > ent.Comp.MaxGeneticDamage)
        {
            Speak(ent, "genetic-damage");
            return;
        }

        // should never happen, no message
        if (_disk.GetDisk(ent.Owner)?.Comp.Mutation is not {} diskMutation ||
            _genome.GetSequence(mob, args.Index) is not {} sequence ||
            _mutation.GetMutatable(mob) is not {} mutatable)
            return;

        var mutation = sequence.Mutation;
        if (_mutation.CombineMutations(mutation, diskMutation) is not {} result)
        {
            Speak(ent, "combine-none");
            return;
        }

        // already present or couldn't add it
        if (!_mutation.AddMutation(mutatable.AsNullable(), result))
        {
            Speak(ent, "combine-present");
            return;
        }

        _damage.ChangeDamage(mob, ent.Comp.CombineDamage);

        Speak(ent, "combined");

        _adminLog.Add(LogType.Genetics, LogImpact.Medium, $"{result} combined from {mutation} and {diskMutation} by {ToPrettyString(args.User)} using console {ToPrettyString(ent)}");

        // it isn't discovered so you have to figure out what it is before it's too late...
        _genome.TryAddSequence(mob, result);
    }

    private void OnCombineCheck(Entity<GeneticsConsoleComponent> ent, ref DoAfterAttemptEvent<CombineDoAfterEvent> args)
    {
        var mob = GetEntity(args.Event.Mob);
        if (!CanKeepWorkingOn(ent, mob) || _disk.GetDisk(ent.Owner) == null)
            args.Cancel();
    }

    private void OnPrint(Entity<GeneticsConsoleComponent> ent, ref GeneticsConsolePrintMessage args)
    {
        var now = _timing.CurTime;
        var i = (int) args.Print;
        if (now < ent.Comp.NextPrint ||
            i >= ent.Comp.Prints.Count ||
            _disk.GetDisk(ent.Owner) is not {} disk ||
            disk.Comp.Mutation is not {} mutation)
            return;

        var delay = ent.Comp.Prints[i].Delay;
        ent.Comp.NextPrint = now + delay;
        DirtyField(ent, ent.Comp, nameof(GeneticsConsoleComponent.NextPrint));

        var proto = ent.Comp.Prints[i].Proto;
        var item = PredictedSpawnAtPosition(proto, Transform(ent).Coordinates);
        _mutator.AddMutation(item, mutation);
        _audio.PlayPredicted(ent.Comp.PrintSound, ent, args.Actor);

        _adminLog.Add(LogType.Genetics, LogImpact.Medium, $"Printed {ToPrettyString(item)} with {mutation} by {ToPrettyString(args.Actor)} using console {ToPrettyString(ent)}");
    }

    private void OnUIOpened(Entity<GeneticsConsoleComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUI(ent);
    }

    private void SetBusy(Entity<GeneticsConsoleComponent> ent, bool busy)
    {
        if (ent.Comp.Busy == busy)
            return;

        ent.Comp.Busy = busy;
        DirtyField(ent, nameof(GeneticsConsoleComponent.Busy));
    }

    private void Speak(EntityUid uid, string suffix)
    {
        var msg = Loc.GetString("genetics-console-chat-" + suffix);
        var type = InGameICChatType.Speak;
        _chat.TrySendInGameICMessage(uid, msg, type, hideChat: false, hideLog: true);
    }

    private void UpdateUI(Entity<GeneticsConsoleComponent> ent)
    {
        _sequences.Clear();
        if (ent.Comp.ScannedMob is {} mob)
            _genome.AddSequenceStates(mob, _sequences);
        var state = new GeneticsConsoleState(_sequences);
        _ui.SetUiState(ent.Owner, GeneticsConsoleUiKey.Key, state);
    }

    #region Public API

    public Chromosome RandomChromosome() => (Chromosome) _random.Next(0, 4);

    public static char CycleBase(char b, GeneticsCycle cycle)
        => (b, cycle) switch
        {
            (_, GeneticsCycle.Reset) => 'X',
            ('A', GeneticsCycle.Next) => 'C',
            ('C', GeneticsCycle.Next) => 'G',
            ('G', GeneticsCycle.Next) => 'T',
            ('T', GeneticsCycle.Next) => 'X',
            ('X', GeneticsCycle.Next) => 'A',
            ('A', GeneticsCycle.Last) => 'X',
            ('C', GeneticsCycle.Last) => 'A',
            ('G', GeneticsCycle.Last) => 'C',
            ('T', GeneticsCycle.Last) => 'G',
            ('X', GeneticsCycle.Last) => 'T',
            _ => b // how
        };

    public bool CanWorkOn(Entity<GeneticsConsoleComponent> ent, EntityUid mob)
        => !ent.Comp.Busy && CanKeepWorkingOn(ent, mob);

    public bool CanKeepWorkingOn(Entity<GeneticsConsoleComponent> ent, EntityUid mob)
        => ent.Comp.ScannedMob == mob // no bait n switch
            && _mutation.CanMutate(mob)
            && _power.IsPowered(ent.Owner);

    public bool CanScan(Entity<GeneticsConsoleComponent> ent, EntityUid mob)
        => CanWorkOn(ent, mob)
            && !_genome.IsScanned(mob); // can't scan someone multiple times

    public bool TryAddRandomChromosome(Entity<GeneticsConsoleComponent?> ent)
    {
        if (!_query.Resolve(ent, ref ent.Comp))
            return false;

        if (_net.IsServer)
            AddChromosome(ent.AsNullable(), RandomChromosome());
        return true;
    }

    public void AddChromosome(Entity<GeneticsConsoleComponent> ent, Chromosome chromosome, int n = 1)
    {
        ent.Comp.Chromosomes[(int) chromosome] += n;
        DirtyField(ent, nameof(GeneticsConsoleComponent.Chromosomes));
    }

    public void RemoveChromosome(Entity<GeneticsConsoleComponent> ent, Chromosome chromosome, int n = 1)
        => AddChromosome(ent, chromosome, -n);

    /// <summary>
    /// Tries to sequences a mutation, either activating it in the mob or damaging it.
    /// </summary>
    public bool SequenceMutation(Entity<GeneticsConsoleComponent> ent, EntityUid mob, uint index)
    {
        if (!CanWorkOn(ent, mob) ||
            _genome.GetSequence(mob, index) is not {} sequence)
            return false;

        var mutation = sequence.Mutation;
        if (_mutation.GetRoundData(mutation) is not {} data)
            return false;

        if (data.Discovered) // no
            return false;

        if (sequence.Bases != data.Bases)
        {
            var you = Loc.GetString("genetics-console-damages-you");
            var others = Loc.GetString("genetics-console-damages-others");
            _audio.PlayPvs(ent.Comp.SequenceFailSound, ent);
            _popup.PopupPredicted(you, others, ent, mob, PopupType.LargeCaution);
            _damage.ChangeDamage(mob, ent.Comp.SequenceFailDamage);
            return false;
        }

        var ev = new MutationSequencedEvent(mutation, data);
        RaiseLocalEvent(ent, ref ev);

        _audio.PlayPvs(ent.Comp.ScanSound, ent);
        data.Discovered = true;
        _mutation.AddMutation(mob, sequence.Mutation);
        UpdateUI(ent); // it's now discovered
        return true;
    }

    #endregion Public API
}

[Serializable, NetSerializable]
public sealed partial class ScanDoAfterEvent : DoAfterEvent
{
    public NetEntity Mob;

    public ScanDoAfterEvent(NetEntity mob)
    {
        Mob = mob;
    }

    public override DoAfterEvent Clone()
        => new ScanDoAfterEvent(Mob);
}

[Serializable, NetSerializable]
public sealed partial class SequenceDoAfterEvent : DoAfterEvent
{
    public NetEntity Mob;
    public uint Index;

    public SequenceDoAfterEvent(NetEntity mob, uint index)
    {
        Mob = mob;
        Index = index;
    }

    public override DoAfterEvent Clone()
        => new SequenceDoAfterEvent(Mob, Index);
}

[Serializable, NetSerializable]
public sealed partial class CombineDoAfterEvent : DoAfterEvent
{
    public NetEntity Mob;
    public uint Index;

    public CombineDoAfterEvent(NetEntity mob, uint index)
    {
        Mob = mob;
        Index = index;
    }

    public override DoAfterEvent Clone()
        => new CombineDoAfterEvent(Mob, Index);
}
