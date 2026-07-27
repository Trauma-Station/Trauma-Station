// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.AlertLevel;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Station;
using System.Linq;

namespace Content.Trauma.Shared.Lockers;

public sealed partial class StationAlertLevelLockSystem : EntitySystem
{
    [Dependency] private AlertLevelSystem _level = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStationSystem _station = default!;
    [Dependency] private EntityQuery<LockComponent> _lockQuery = default!;

    [SubscribeLocalEvent]
    public void OnInit(Entity<StationAlertLevelLockComponent> ent, ref MapInitEvent args)
    {
        // for non-station mapped safes don't lock them because that's chuddy
        if (_station.GetOwningStation(ent.Owner) is not { } station ||
            !_level.TryGetLevel(station, out var level))
        {
            ent.Comp.Enabled = false;
            Dirty(ent);
            return;
        }

        ent.Comp.StationId = station;
        ent.Comp.Enabled = true;

        CheckAlertLevels(ent, level.Value);
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnAlertChanged(ref AlertLevelChangedEvent args)
    {
        var query = EntityQueryEnumerator<StationAlertLevelLockComponent>();
        var station = args.Station;
        foreach (var ent in query)
        {
            if (station != ent.Comp.StationId)
                continue;

            CheckAlertLevels(ent, args.AlertLevel);
        }
    }

    [SubscribeLocalEvent]
    private void OnLockToggleAttempt(Entity<StationAlertLevelLockComponent> ent, ref LockToggleAttemptEvent args)
    {
        if (!ent.Comp.Enabled || !ent.Comp.Locked ||
            !_lockQuery.TryComp(ent, out var lockComp) ||
            !lockComp.Locked) // Allow locking even if the alert level is wrong
            return;

        if (!args.Silent)
            _popup.PopupEntity(Loc.GetString("access-failed-wrong-station-alert-level"), ent, args.User);

        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnEmagged(Entity<StationAlertLevelLockComponent> ent, ref GotEmaggedEvent args)
    {
        // don't waste multiple emag charges
        if (!ent.Comp.Enabled)
            return;

        args.Handled = true;
        ent.Comp.Enabled = false;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<StationAlertLevelLockComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.Enabled || ent.Comp.LockedAlertLevels.Count == 0)
            return;

        var levels = string.Join(", ", ent.Comp.LockedAlertLevels.Select(id => ProtoMan.Index(id).LocalizedName.ToLowerInvariant()));
        args.PushMarkup(Loc.GetString("station-alert-level-lock-examined", ("levels", levels)));
    }

    private void CheckAlertLevels(Entity<StationAlertLevelLockComponent> ent, ProtoId<AlertLevelPrototype> id)
    {
        ent.Comp.Locked = ent.Comp.LockedAlertLevels.Contains(id);
        Dirty(ent);
    }
}
