// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Genetics.Console;

public sealed partial class GeneticsConsoleSystem
{
    [Dependency] private readonly UniqueEnzymesSystem _enzymes = default!;

    private void InitializeEnzymes()
    {
        SubscribeLocalEvent<GeneticsConsoleEnzymesComponent, MapInitEvent>(OnEnzymesMapInit);
        SubscribeLocalEvent<GeneticsConsoleEnzymesComponent, ApplyEnzymesDoAfterEvent>(OnApplyEnzymesDoAfter);
        SubscribeLocalEvent<GeneticsConsoleEnzymesComponent, DoAfterAttemptEvent<ApplyEnzymesDoAfterEvent>>(OnApplyEnzymesCheck);
        Subs.BuiEvents<GeneticsConsoleEnzymesComponent>(GeneticsConsoleUiKey.Key, subs =>
        {
            subs.Event<GeneticsConsoleSaveEnzymesMessage>(OnSaveEnzymes);
            subs.Event<GeneticsConsoleApplyEnzymesMessage>(OnApplyEnzymes);
        });
    }

    private void OnEnzymesMapInit(Entity<GeneticsConsoleEnzymesComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextApply = _timing.CurTime + ent.Comp.ApplyDelay;
        Dirty(ent);
    }

    private void OnSaveEnzymes(Entity<GeneticsConsoleEnzymesComponent> ent, ref GeneticsConsoleSaveEnzymesMessage args)
    {
        if (GetWorkableMob(ent.Owner) is not {} mob ||
            _disk.GetDisk(ent.Owner) is not {} disk)
            return;

        var name = Name(mob);
        if (disk.Comp.Enzymes?.Name == name) // do nothing if it's the same as on disk
            return;

        _disk.SetEnzymes(disk, _enzymes.GetEnzymes(mob));

        _adminLog.Add(LogType.Genetics, LogImpact.Low, $"{ToPrettyString(args.Actor)} saved {name}'s unique enzymes to {ToPrettyString(disk)} with console {ToPrettyString(ent)}");

        _audio.PlayPredicted(ent.Comp.SaveSound, ent, args.Actor);
    }

    private void OnApplyEnzymes(Entity<GeneticsConsoleEnzymesComponent> ent, ref GeneticsConsoleApplyEnzymesMessage args)
    {
        if (_timing.CurTime < ent.Comp.NextApply ||
            _disk.GetDisk(ent.Owner)?.Comp.Enzymes == null ||
            GetWorkableMob(ent.Owner) is not {} mob ||
            !_genome.IsScanned(mob)) // need to scan before applying unique enzymes
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.Actor,
            ent.Comp.ApplyDuration,
            new ApplyEnzymesDoAfterEvent(GetNetEntity(mob)),
            eventTarget: ent,
            target: mob,
            used: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            AttemptFrequency = AttemptFrequency.EveryTick
        };
        SetBusy(ent.Owner, _doAfter.TryStartDoAfter(doAfterArgs));
        Speak(ent, "applying-enzymes");
    }

    private void OnApplyEnzymesDoAfter(Entity<GeneticsConsoleEnzymesComponent> ent, ref ApplyEnzymesDoAfterEvent args)
    {
        SetBusy(ent.Owner, false);
        if (args.Cancelled)
        {
            Speak(ent, "apply-enzymes-failed");
            return;
        }

        args.Handled = true;
        var mob = GetEntity(args.Mob);
        if (!CanWorkOn(ent.Owner, mob))
        {
            Speak(ent, "apply-enzymes-failed");
            return;
        }

        var damage = _mutation.GetGeneticDamage(mob) ?? 0;
        if (damage > ent.Comp.MaxGeneticDamage)
        {
            Speak(ent, "genetic-damage");
            return;
        }

        var now = _timing.CurTime;
        if (now < ent.Comp.NextApply || _disk.GetDisk(ent.Owner)?.Comp.Enzymes is not {} enzymes)
            return; // should be impossible

        ent.Comp.NextApply = now + ent.Comp.ApplyDelay;
        Dirty(ent);

        Speak(ent, "applied-enzymes");
        _enzymes.ChangeEnzymes(mob, enzymes);

        _adminLog.Add(LogType.Genetics, LogImpact.High, $"{ToPrettyString(args.User)} applied unique enzymes of '{enzymes.Name}' to {ToPrettyString(mob)} with console {ToPrettyString(ent)}");

        _audio.PlayPredicted(ent.Comp.ApplySound, ent, args.User);
    }

    private void OnApplyEnzymesCheck(Entity<GeneticsConsoleEnzymesComponent> ent, ref DoAfterAttemptEvent<ApplyEnzymesDoAfterEvent> args)
    {
        var mob = GetEntity(args.Event.Mob);
        if (!CanKeepWorkingOn(ent.Owner, mob))
            args.Cancel();
    }
}

[Serializable, NetSerializable]
public sealed partial class ApplyEnzymesDoAfterEvent : DoAfterEvent
{
    public NetEntity Mob;

    public ApplyEnzymesDoAfterEvent(NetEntity mob)
    {
        Mob = mob;
    }

    public override DoAfterEvent Clone()
        => new ApplyEnzymesDoAfterEvent(Mob);
}
