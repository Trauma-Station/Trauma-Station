// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Database;
using Content.Trauma.Shared.Genetics.Tools;

namespace Content.Trauma.Shared.Genetics.Console;

public sealed partial class GeneticsConsoleSystem
{
    [Dependency] private readonly EnzymeIncubatorSystem _incubator = default!;
    [Dependency] private readonly UniqueEnzymesSystem _enzymes = default!;

    private void InitializeEnzymes()
    {
        SubscribeLocalEvent<GeneticsConsoleEnzymesComponent, MapInitEvent>(OnEnzymesMapInit);
        Subs.BuiEvents<GeneticsConsoleEnzymesComponent>(GeneticsConsoleUiKey.Key, subs =>
        {
            subs.Event<GeneticsConsoleSaveEnzymesMessage>(OnSaveEnzymes);
            subs.Event<GeneticsConsolePrintIncubatorMessage>(OnPrintIncubator);
        });
    }

    private void OnEnzymesMapInit(Entity<GeneticsConsoleEnzymesComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextPrint = _timing.CurTime + ent.Comp.PrintDelay;
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

    private void OnPrintIncubator(Entity<GeneticsConsoleEnzymesComponent> ent, ref GeneticsConsolePrintIncubatorMessage args)
    {
        var now = _timing.CurTime;
        if (now < ent.Comp.NextPrint ||
            _disk.GetDisk(ent.Owner) is not {} disk ||
            disk.Comp.Enzymes is not {} enzymes)
            return;

        ent.Comp.NextPrint = now + ent.Comp.PrintDelay;
        Dirty(ent);

        var item = PredictedSpawnAtPosition(ent.Comp.Incubator, Transform(ent).Coordinates);
        _incubator.SetEnzymes(item, enzymes);

        _adminLog.Add(LogType.Genetics, LogImpact.Low, $"{ToPrettyString(args.Actor)} printed {enzymes.Name}'s unique enzymes to {ToPrettyString(item)} with console {ToPrettyString(ent)}");

        _audio.PlayPredicted(ent.Comp.PrintSound, ent, args.Actor);
    }
}
