using Content.Shared.Chat;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.UserInterface;
using Content.Trauma.Common.Medical;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using System.Text;

namespace Content.Trauma.Shared.Genetics.Console;

// TODO: admin log actions
public sealed class GeneticsConsoleSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private StringBuilder _builder = new();
    private List<SequenceState> _sequences = new();

    private EntityQuery<GeneticsConsoleComponent> _query;
    private EntityQuery<GeneticsDiskComponent> _diskQuery;
    private EntityQuery<MutatableComponent> _mutatableQuery;
    private EntityQuery<ScannedGenomeComponent> _scannedQuery;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<GeneticsConsoleComponent>();
        _diskQuery = GetEntityQuery<GeneticsDiskComponent>();
        _mutatableQuery = GetEntityQuery<MutatableComponent>();
        _scannedQuery = GetEntityQuery<ScannedGenomeComponent>();

        SubscribeLocalEvent<GeneticsConsoleComponent, ScannerConnectedEvent>(OnScannerConnected);
        SubscribeLocalEvent<GeneticsConsoleComponent, ScannerDisconnectedEvent>(OnScannerDisconnected);
        SubscribeLocalEvent<GeneticsConsoleComponent, ScannerInsertedEvent>(OnScannerInserted);
        SubscribeLocalEvent<GeneticsConsoleComponent, ScannerEjectedEvent>(OnScannerEjected);
        SubscribeLocalEvent<GeneticsConsoleComponent, ScanDoAfterEvent>(OnScanDoAfter);
        SubscribeLocalEvent<GeneticsConsoleComponent, DoAfterAttemptEvent<ScanDoAfterEvent>>(OnScanCheck);
        SubscribeLocalEvent<GeneticsConsoleComponent, SequenceDoAfterEvent>(OnSequenceDoAfter);
        SubscribeLocalEvent<GeneticsConsoleComponent, DoAfterAttemptEvent<SequenceDoAfterEvent>>(OnSequenceCheck);
        SubscribeLocalEvent<GeneticsConsoleComponent, AfterActivatableUIOpenEvent>(OnUIOpened);

        Subs.BuiEvents<GeneticsConsoleComponent>(GeneticsConsoleUiKey.Key, subs =>
        {
            subs.Event<GeneticsConsoleScanMessage>(OnScan);
            subs.Event<GeneticsConsoleSetBaseMessage>(OnSetBase);
            subs.Event<GeneticsConsoleJokerMessage>(OnJoker);
            subs.Event<GeneticsConsoleSequenceMessage>(OnSequence);
            subs.Event<GeneticsConsoleWriteMutationMessage>(OnWriteMutation);
        });
    }

    private void OnScannerConnected(Entity<GeneticsConsoleComponent> ent, ref ScannerConnectedEvent args)
    {
        ent.Comp.Scanner = args.Scanner;
        DirtyField(ent, nameof(GeneticsConsoleComponent.Scanner));
    }

    private void OnScannerDisconnected(Entity<GeneticsConsoleComponent> ent, ref ScannerDisconnectedEvent args)
    {
        ent.Comp.Scanner = null;
        DirtyField(ent, nameof(GeneticsConsoleComponent.Scanner));
    }

    private void OnScannerInserted(Entity<GeneticsConsoleComponent> ent, ref ScannerInsertedEvent args)
    {
        ent.Comp.ScannedMob = args.Target;
        DirtyField(ent, nameof(GeneticsConsoleComponent.ScannedMob));
    }

    private void OnScannerEjected(Entity<GeneticsConsoleComponent> ent, ref ScannerEjectedEvent args)
    {
        ent.Comp.ScannedMob = null;
        DirtyField(ent, nameof(GeneticsConsoleComponent.ScannedMob));
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

        var scanned = EnsureComp<ScannedGenomeComponent>(mob);

        if (_net.IsClient) return;

        _audio.PlayPvs(ent.Comp.ScanSound, ent);
        var mutatable = _mutatableQuery.Comp(mob);
        foreach (var id in mutatable.Dormant)
        {
            TryAddSequence(scanned, id);
        }
        UpdateUI(ent);
        Speak(ent, "scanned");
    }

    private void OnScanCheck(Entity<GeneticsConsoleComponent> ent, ref DoAfterAttemptEvent<ScanDoAfterEvent> args)
    {
        var mob = GetEntity(args.Event.Mob);
        if (!CanKeepWorkingOn(ent, mob))
            args.Cancel();
    }

    private void TryAddSequence(ScannedGenomeComponent comp, EntProtoId<MutationComponent> id)
    {
        if (comp.Sequences.Count >= ScannedGenomeComponent.SequenceLimit ||
            !_mutation.AllMutations.TryGetValue(id, out var mutation) ||
            _mutation.GetRoundData(id) is not {} data)
        {
            return;
        }

        _builder.Clear();
        _builder.Append(data.Bases);

        // give difficulty a random offset so its a bit harder to metagame what a mutation could be
        // you can still generally go off more bases missing = better but not automatically know
        // exactly what it is by grepping the mutations :)
        var difficulty = mutation.Difficulty;
        difficulty += _random.Next(-2, 2);
        difficulty = Math.Clamp(difficulty, 0, MutationData.BaseCount);

        // chance of Xing out a whole pair goes up with difficulty
        // so you are less likely to get free easy fixes
        var pairChance = (float) difficulty / MutationData.BaseCount;

        // randomly X out bases depending on mutation difficulty
        while (difficulty > 0)
        {
            var pair = _random.Next(0, MutationData.PairCount);
            var i = pair * 2;
            // cant X out a whole pair if theres only 1 difficulty left
            if (difficulty >= 2 && _random.Prob(pairChance))
            {
                TryX(i);
                TryX(i + 1);
            }
            else if (_random.Prob(0.5f))
            {
                TryX(i);
            }
            else
            {
                TryX(i + 1);
            }
        }
        comp.Sequences.Add(new Sequence
        {
            Mutation = id,
            Bases = _builder.ToString()
        });

        void TryX(int i)
        {
            if (_builder[i] == 'X')
                return;

            _builder[i] = 'X';
            difficulty--;
        }
    }

    private void OnSetBase(Entity<GeneticsConsoleComponent> ent, ref GeneticsConsoleSetBaseMessage args)
    {
        if (ent.Comp.ScannedMob is not {} mob || !CanWorkOn(ent, mob))
            return;

        if (GetSequence(mob, args.Sequence) is not {} sequence)
            return;

        var valid = args.Base switch
        {
            'A' => true,
            'C' => true,
            'G' => true,
            'T' => true,
            'X' => true,
            _ => false
        };
        if (!valid)
            return;

        // chud language can't just set a char directly
        _builder.Clear();
        _builder.Append(sequence.Bases);
        _builder[(int) args.Index] = args.Base;
        sequence.Bases = _builder.ToString();
        UpdateUI(ent);
    }

    private void OnJoker(Entity<GeneticsConsoleComponent> ent, ref GeneticsConsoleJokerMessage args)
    {
        if (ent.Comp.ScannedMob is not {} mob)
            return;

        if (!CanWorkOn(ent, mob))
            return;

        if (GetSequence(mob, args.Index) is not {} sequence)
            return;

        // TODO
    }

    private void OnSequence(Entity<GeneticsConsoleComponent> ent, ref GeneticsConsoleSequenceMessage args)
    {
        if (ent.Comp.ScannedMob is not {} mob)
            return;

        if (!CanWorkOn(ent, mob) || GetSequence(mob, args.Index) is not {} sequence)
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
        if (_net.IsClient)
            return;

        if (GetDisk(ent) is not {} disk)
            return;

        if (ent.Comp.ScannedMob is not {} mob || GetSequence(mob, args.Index) is not {} sequence)
            return;

        var mutation = sequence.Mutation;
        if (_mutation.GetRoundData(mutation)?.Discovered != true)
            return;

        // TODO admin log
        _audio.PlayPvs(ent.Comp.WriteSound, ent);
        disk.Comp.Mutation = mutation;
        Dirty(disk);
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
        if (_scannedQuery.TryComp(ent.Comp.ScannedMob, out var scanned))
        {
            foreach (var sequence in scanned.Sequences)
            {
                var id = sequence.Mutation;
                if (_mutation.GetRoundData(id) is not {} data)
                {
                    Log.Error($"Sequence of {ToPrettyString(ent.Comp.ScannedMob)} contains unknown mutation {id}!");
                    continue;
                }

                _sequences.Add(new SequenceState(sequence.Bases, data.Number, data.Discovered ? id : null));
            }
        }
        var state = new GeneticsConsoleState(_sequences);
        _ui.SetUiState(ent.Owner, GeneticsConsoleUiKey.Key, state);
    }

    #region Public API

    public Chromosome RandomChromosome() => (Chromosome) _random.Next(0, 4);

    public bool CanWorkOn(Entity<GeneticsConsoleComponent> ent, EntityUid mob)
        => !ent.Comp.Busy && CanKeepWorkingOn(ent, mob);

    public bool CanKeepWorkingOn(Entity<GeneticsConsoleComponent> ent, EntityUid mob)
        => ent.Comp.ScannedMob == mob // no bait n switch
            && _mutation.CanMutate(mob)
            && _power.IsPowered(ent.Owner);

    public bool CanScan(Entity<GeneticsConsoleComponent> ent, EntityUid mob)
        => CanWorkOn(ent, mob)
            && !_scannedQuery.HasComp(mob); // can't scan someone multiple times

    public Entity<GeneticsDiskComponent>? GetDisk(Entity<GeneticsConsoleComponent> ent)
    {
        if (_slots.GetItemOrNull(ent.Owner, ent.Comp.DiskSlot) is not {} item)
            return null;

        if (!_diskQuery.TryComp(item, out var disk))
            return null;

        return (item, disk);
    }

    public void SetDiskMutation(Entity<GeneticsDiskComponent> ent, EntProtoId<MutationComponent>? id)
    {
        ent.Comp.Mutation = id;
        Dirty(ent);
    }

    public Sequence? GetSequence(EntityUid mob, uint index)
        => _scannedQuery.TryComp(mob, out var scanned)
            && index < scanned.Sequences.Count
            ? scanned.Sequences[(int) index]
            : null;

    public bool TryAddRandomChromosome(Entity<GeneticsConsoleComponent?> ent)
    {
        if (!_query.Resolve(ent, ref ent.Comp))
            return false;

        if (_net.IsServer)
            AddChromosome((ent, ent.Comp), RandomChromosome());
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
            GetSequence(mob, index) is not {} sequence)
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
            _damage.TryChangeDamage(mob, ent.Comp.SequenceFailDamage);
            return false;
        }

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
