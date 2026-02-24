// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.UserInterface;
using Content.Trauma.Common.Medical;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Genetics.Console;

public sealed partial class GeneticsConsoleSystem
{
    private List<SequenceState> _sequences = new();

    private EntityQuery<GeneticsScannerComponent> _scannerQuery;

    private void InitializeScanner()
    {
        _scannerQuery = GetEntityQuery<GeneticsScannerComponent>();

        SubscribeLocalEvent<GeneticsScannerComponent, ScannerConnectedEvent>(OnScannerConnected);
        SubscribeLocalEvent<GeneticsScannerComponent, ScannerDisconnectedEvent>(OnScannerDisconnected);
        SubscribeLocalEvent<GeneticsScannerComponent, ScannerInsertedEvent>(OnScannerInserted);
        SubscribeLocalEvent<GeneticsScannerComponent, ScannerEjectedEvent>(OnScannerEjected);
        SubscribeLocalEvent<GeneticsScannerComponent, ScanDoAfterEvent>(OnScanDoAfter);
        SubscribeLocalEvent<GeneticsScannerComponent, DoAfterAttemptEvent<ScanDoAfterEvent>>(OnScanCheck);
        SubscribeLocalEvent<GeneticsScannerComponent, AfterActivatableUIOpenEvent>(OnUIOpened);
        Subs.BuiEvents<GeneticsScannerComponent>(GeneticsConsoleUiKey.Key, subs =>
        {
            subs.Event<GeneticsConsoleScanMessage>(OnScan);
        });
    }

    #region Event Handlers

    private void OnScannerConnected(Entity<GeneticsScannerComponent> ent, ref ScannerConnectedEvent args)
    {
        SetScanner(ent.AsNullable(), args.Scanner);
    }

    private void OnScannerDisconnected(Entity<GeneticsScannerComponent> ent, ref ScannerDisconnectedEvent args)
    {
        SetScanner(ent.AsNullable(), null);
    }

    private void OnScannerInserted(Entity<GeneticsScannerComponent> ent, ref ScannerInsertedEvent args)
    {
        SetScannedMob(ent.AsNullable(), args.Target);
    }

    private void OnScannerEjected(Entity<GeneticsScannerComponent> ent, ref ScannerEjectedEvent args)
    {
        SetScannedMob(ent.AsNullable(), null);
    }

    private void OnScan(Entity<GeneticsScannerComponent> ent, ref GeneticsConsoleScanMessage args)
    {
        if (ent.Comp.ScannedMob is not {} mob)
            return;

        if (!CanScan(ent.AsNullable(), mob))
            return;

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            args.Actor,
            ent.Comp.ScanDelay,
            new ScanDoAfterEvent(GetNetEntity(mob)),
            eventTarget: ent,
            target: mob,
            used: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            AttemptFrequency = AttemptFrequency.EveryTick
        };
        SetBusy(ent.AsNullable(), _doAfter.TryStartDoAfter(doAfterArgs));

        Speak(ent, "scanning");
    }

    private void OnScanDoAfter(Entity<GeneticsScannerComponent> ent, ref ScanDoAfterEvent args)
    {
        SetBusy(ent.AsNullable(), false);
        if (args.Cancelled)
        {
            Speak(ent, "scan-failed");
            return;
        }

        args.Handled = true;
        var mob = GetEntity(args.Mob);
        if (!CanScan(ent.AsNullable(), mob))
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
        UpdateUI(ent.AsNullable());
    }

    private void OnScanCheck(Entity<GeneticsScannerComponent> ent, ref DoAfterAttemptEvent<ScanDoAfterEvent> args)
    {
        var mob = GetEntity(args.Event.Mob);
        if (!CanKeepWorkingOn(ent.AsNullable(), mob))
            args.Cancel();
    }

    private void OnUIOpened(Entity<GeneticsScannerComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUI(ent.AsNullable());
    }

    #endregion

    private void SetScanner(Entity<GeneticsScannerComponent?> ent, EntityUid? scanner)
    {
        if (!_scannerQuery.Resolve(ent, ref ent.Comp, false) || ent.Comp.Scanner == scanner)
            return;

        ent.Comp.Scanner = scanner;
        DirtyField(ent, nameof(GeneticsScannerComponent.Scanner));
        UpdateUI(ent);
    }

    private void SetScannedMob(Entity<GeneticsScannerComponent?> ent, EntityUid? mob)
    {
        if (!_scannerQuery.Resolve(ent, ref ent.Comp) || ent.Comp.ScannedMob == mob)
            return;

        ent.Comp.ScannedMob = mob;
        DirtyField(ent, nameof(GeneticsScannerComponent.ScannedMob));
        UpdateUI(ent);
    }

    private void SetBusy(Entity<GeneticsScannerComponent?> ent, bool busy)
    {
        if (!_scannerQuery.Resolve(ent, ref ent.Comp) || ent.Comp.Busy == busy)
            return;

        ent.Comp.Busy = busy;
        DirtyField(ent, nameof(GeneticsScannerComponent.Busy));
    }

    private void UpdateUI(Entity<GeneticsScannerComponent?> ent)
    {
        if (!_scannerQuery.Resolve(ent, ref ent.Comp))
            return;

        _sequences.Clear();
        if (ent.Comp.ScannedMob is {} mob)
            _genome.AddSequenceStates(mob, _sequences);
        var state = new GeneticsConsoleState(_sequences);
        _ui.SetUiState(ent.Owner, GeneticsConsoleUiKey.Key, state);
    }

    #region Public API

    public bool CanWorkOn(Entity<GeneticsScannerComponent?> ent, EntityUid mob)
        => _scannerQuery.Resolve(ent, ref ent.Comp) &&
            !ent.Comp.Busy &&
            CanKeepWorkingOn(ent, mob);

    public bool CanKeepWorkingOn(Entity<GeneticsScannerComponent?> ent, EntityUid mob)
        => _scannerQuery.Resolve(ent, ref ent.Comp) &&
            ent.Comp.ScannedMob == mob && // no bait n switch
            _mutation.CanMutate(mob) &&
            _power.IsPowered(ent.Owner);

    public bool CanScan(Entity<GeneticsScannerComponent?> ent, EntityUid mob)
        => CanWorkOn(ent, mob)
            && !_genome.IsScanned(mob); // can't scan someone multiple times

    /// <summary>
    /// Returns a mob if one is in the scanner.
    /// </summary>
    public EntityUid? GetScannedMob(Entity<GeneticsScannerComponent?> ent)
        => _scannerQuery.Resolve(ent, ref ent.Comp) && ent.Comp.ScannedMob is {} mob
            ? mob
            : null;

    /// <summary>
    /// Returns a mob if one is in the scanner and it can be worked on.
    /// </summary>
    public EntityUid? GetWorkableMob(Entity<GeneticsScannerComponent?> ent)
        => GetScannedMob(ent) is {} mob && CanWorkOn(ent, mob)
            ? mob
            : null;

    #endregion
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
