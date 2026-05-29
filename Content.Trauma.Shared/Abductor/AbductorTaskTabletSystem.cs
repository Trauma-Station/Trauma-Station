// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Abductor;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.Abductor;

/// <summary>
/// Handles all interactions with the task tablet.
/// </summary>
public sealed partial class AbductorTaskTabletSystem : EntitySystem
{
    [Dependency] private AbductorTaskSystem _task = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AbductorTaskTabletComponent, AfterInteractEvent>(OnAfterInteract);
        Subs.BuiEvents<AbductorTaskTabletComponent>(AbductorTaskTabletUIKey.Key, subs =>
        {
            subs.Event<AbductorTaskScanMessage>(OnScan);
            subs.Event<AbductorTaskCompleteMessage>(OnComplete);
        });
    }

    private void OnAfterInteract(Entity<AbductorTaskTabletComponent> ent, ref AfterInteractEvent args)
    {
        var user = args.User;
        if (args.Handled ||
            !args.CanReach ||
            args.Target is not {} target ||
            target == user) // lol no
            return;

        // have to gizmo + abduct first chud
        if (!HasComp<AbductorVictimComponent>(target))
        {
            _popup.PopupClient(Loc.GetString("abductor-task-tablet-not-abducted"), target, user);
            return;
        }

        args.Handled = true;
        if (_task.AllTasksCompleted(target))
        {
            _popup.PopupClient(Loc.GetString("abductor-task-tablet-already-completed"), target, user);
            return;
        }

        var netTarget = GetNetEntity(target);
        if (netTarget == ent.Comp.Target)
            return;

        ent.Comp.Target = netTarget;
        Dirty(ent);

        _popup.PopupClient(Loc.GetString("abductor-task-tablet-linked"), target, user);
        _ui.TryOpenUi(ent.Owner, AbductorTaskTabletUIKey.Key, user);
    }

    private void OnScan(Entity<AbductorTaskTabletComponent> ent, ref AbductorTaskScanMessage args)
    {
        if (GetEntity(ent.Comp.Target) is not {} target ||
            !InRange(ent, target) ||
            _task.IsSubject(target)) // no sound spamming
            return;

        var user = args.Actor;
        _adminLog.Add(LogType.AntagObjective, $"Abductor tasks created for {ToPrettyString(target)} by {ToPrettyString(user)}");

        EnsureComp<AbductorSubjectComponent>(target);
        _audio.PlayPredicted(ent.Comp.ScanSound, ent, user);
    }

    private void OnComplete(Entity<AbductorTaskTabletComponent> ent, ref AbductorTaskCompleteMessage args)
    {
        if (GetEntity(ent.Comp.Target) is not {} target ||
            !InRange(ent, target) ||
            !TryComp<AbductorSubjectComponent>(target, out var subject))
            return;

        var user = args.Actor;
        var task = subject.NextTask;
        if (!_task.TryCompleteTask((target, subject)))
        {
            _popup.PopupClient(Loc.GetString("abductor-task-tablet-incomplete"), user, user);
            return;
        }

        _adminLog.Add(LogType.AntagObjective, $"Abductor task {task} completed on {ToPrettyString(target)} by {ToPrettyString(user)}");

        var ev = new AbductorTaskCompleteEvent();
        RaiseLocalEvent(user, ref ev);

        if (!_task.AllTasksCompleted((target, subject)))
            return;

        _audio.PlayPredicted(ent.Comp.FinishSound, ent, user);
        _popup.PopupClient(Loc.GetString("abductor-task-tablet-finished"), user, user);

        ent.Comp.Target = null;
        Dirty(ent);
        _ui.CloseUi(ent.Owner, AbductorTaskTabletUIKey.Key, user);
    }

    public bool InRange(Entity<AbductorTaskTabletComponent> ent, EntityUid target)
    {
        var xform = Transform(ent);
        var targetXform = Transform(target);
        return _transform.InRange(xform.Coordinates, targetXform.Coordinates, ent.Comp.Range);
    }
}

/// <summary>
/// Raised on the abductor mob whenever a task from an experiment is completed.
/// </summary>
[ByRefEvent]
public record struct AbductorTaskCompleteEvent();
