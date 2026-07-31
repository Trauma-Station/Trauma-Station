// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Administration.Logs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.UserInterface;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.ChemiCompiler;

/// <summary>
/// Handles the ChemiCompiler's interface: saving programs, loading beakers, and starting and stopping runs.
/// Actually running a program is done by the server, see ChemiCompilerRunnerSystem.
/// </summary>
public sealed partial class ChemiCompilerSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        // the generator has no attribute for bui events, so this one subscription stays written out
        Subs.BuiEvents<ChemiCompilerComponent>(ChemiCompilerUiKey.Key, subs =>
        {
            subs.Event<ChemiCompilerSaveMessage>(OnSave);
            subs.Event<ChemiCompilerRunMessage>(OnRun);
            subs.Event<ChemiCompilerReservoirMessage>(OnReservoir);
            subs.Event<ChemiCompilerHaltMessage>(OnHalt);
        });
    }

    [SubscribeLocalEvent]
    private void OnUiOpen(Entity<ChemiCompilerComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnReservoirChanged(Entity<ChemiCompilerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnReservoirChanged(Entity<ChemiCompilerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<ChemiCompilerComponent> ent, ref PowerChangedEvent args)
    {
        // pulling the plug on a running program stops it wherever it got to
        if (!args.Powered)
            Halt(ent, "chemicompiler-halted-power");
    }

    private void OnSave(Entity<ChemiCompilerComponent> ent, ref ChemiCompilerSaveMessage args)
    {
        if (HasComp<ActiveChemiCompilerComponent>(ent) || !ValidSlot(ent, args.Slot))
            return;

        if (ChemFuck.Validate(args.Code, ent.Comp.MaxProgramLength) is { } error)
        {
            _popup.PopupCursor(error, args.Actor, PopupType.MediumCaution);
            _audio.PlayPvs(ent.Comp.FailSound, ent);
            return;
        }

        // saving over a locked program is allowed, you just can't read the old one back first
        ent.Comp.Programs[args.Slot] = args.Code;
        ent.Comp.Locked[args.Slot] = false;

        UpdateUi(ent);
    }

    private void OnRun(Entity<ChemiCompilerComponent> ent, ref ChemiCompilerRunMessage args)
    {
        if (HasComp<ActiveChemiCompilerComponent>(ent) || !ValidSlot(ent, args.Slot))
            return;

        if (!_power.IsPowered(ent.Owner))
            return;

        var program = ent.Comp.Programs[args.Slot];
        if (string.IsNullOrEmpty(program))
            return; // empty slot, nothing to run

        // shouldn't fail since saving validates too, but a bad program must never start
        var error = ChemFuck.Validate(program, ent.Comp.MaxProgramLength);
        var jumps = error == null ? ChemFuck.BuildJumpTable(program) : null;
        if (jumps == null)
        {
            _popup.PopupCursor(error ?? Loc.GetString("chemicompiler-error-unmatched-start"), args.Actor, PopupType.MediumCaution);
            _audio.PlayPvs(ent.Comp.FailSound, ent);
            return;
        }

        var active = AddComp<ActiveChemiCompilerComponent>(ent);
        active.Slot = args.Slot;
        active.Program = program;
        active.JumpTable = jumps;
        active.Started = _timing.CurTime;
        active.NextStep = _timing.CurTime;

        // beakers are held in place while the program runs so it can't have the rug pulled out from under it
        SetReservoirsLocked(ent, true);

        _audio.PlayPvs(ent.Comp.StartSound, ent);
        _adminLog.Add(LogType.ChemiCompiler,
            LogImpact.Medium,
            $"{args.Actor:user} ran ChemiCompiler program {args.Slot + 1} ({program.Length} instructions) on {ent.Owner:machine}");

        UpdateUi(ent);
    }

    private void OnReservoir(Entity<ChemiCompilerComponent> ent, ref ChemiCompilerReservoirMessage args)
    {
        if (HasComp<ActiveChemiCompilerComponent>(ent))
            return;

        if (args.Reservoir < 1 || args.Reservoir > ChemiCompilerComponent.Reservoirs)
            return;

        if (!_slots.TryGetSlot(ent, ent.Comp.SlotId(args.Reservoir), out var slot))
            return;

        if (slot.Item == null)
            _slots.TryInsertFromHand(ent, slot, args.Actor);
        else
            _slots.TryEjectToHands(ent, slot, args.Actor, excludeUserAudio: true);
    }

    private void OnHalt(Entity<ChemiCompilerComponent> ent, ref ChemiCompilerHaltMessage args)
    {
        Halt(ent, "chemicompiler-halted-manual");
    }

    /// <summary>
    /// Stops a running program, giving the beakers back and telling the interface why it stopped.
    /// Does nothing if the machine wasn't running.
    /// </summary>
    public void Halt(Entity<ChemiCompilerComponent> ent, string? reason = null)
    {
        if (!TryComp<ActiveChemiCompilerComponent>(ent, out var active))
            return;

        RemComp<ActiveChemiCompilerComponent>(ent);
        SetReservoirsLocked(ent, false);

        _audio.PlayPvs(ent.Comp.IdleSound, ent);

        // the registers stay on screen after halting so you can see where a program got to
        UpdateUi(ent, active);

        if (reason != null)
            _popup.PopupEntity(Loc.GetString(reason), ent);
    }

    /// <summary>
    /// Pushes the current state of the machine to whoever has the interface open.
    /// </summary>
    public void UpdateUi(Entity<ChemiCompilerComponent> ent, ActiveChemiCompilerComponent? active = null)
    {
        // Programs only exist on the server, so a client running this would push a state full of empty slots and
        // blank out the interface. Inserting a beaker is predicted, so the client does reach here.
        if (_net.IsClient)
            return;

        active ??= CompOrNull<ActiveChemiCompilerComponent>(ent);

        var programs = new string?[ChemiCompilerComponent.CodeSlots];
        var filled = new bool[ChemiCompilerComponent.CodeSlots];
        for (var i = 0; i < ChemiCompilerComponent.CodeSlots; i++)
        {
            if (!ValidSlot(ent, i))
                continue;

            var program = ent.Comp.Programs[i];
            filled[i] = !string.IsNullOrEmpty(program);
            // locked programs still light their button up, they just can't be read back out
            programs[i] = ent.Comp.Locked[i] ? null : program;
        }

        var reservoirs = new bool[ChemiCompilerComponent.Reservoirs];
        for (var i = 0; i < ChemiCompilerComponent.Reservoirs; i++)
        {
            reservoirs[i] = _slots.GetItemOrNull(ent, ent.Comp.SlotId(i + 1)) != null;
        }

        var state = new ChemiCompilerState(
            programs,
            filled,
            reservoirs,
            HasComp<ActiveChemiCompilerComponent>(ent),
            active?.Source ?? 0,
            active?.Target ?? 0,
            active?.Amount ?? 0);

        _ui.SetUiState(ent.Owner, ChemiCompilerUiKey.Key, state);
    }

    private void SetReservoirsLocked(Entity<ChemiCompilerComponent> ent, bool locked)
    {
        for (var i = 1; i <= ChemiCompilerComponent.Reservoirs; i++)
        {
            _slots.SetLock(ent, ent.Comp.SlotId(i), locked);
        }
    }

    /// <summary>
    /// Checks a slot index the client sent is one this machine actually has.
    /// The arrays are checked too in case a prototype gave the machine fewer than it should have.
    /// </summary>
    private static bool ValidSlot(Entity<ChemiCompilerComponent> ent, int slot)
        => slot >= 0
            && slot < ChemiCompilerComponent.CodeSlots
            && slot < ent.Comp.Programs.Length
            && slot < ent.Comp.Locked.Length;
}
