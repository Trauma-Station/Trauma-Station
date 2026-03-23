// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Abductor;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.Abductor;

/// <summary>
/// Handles all interactions with the task tablet.
/// </summary>
public sealed class AbductorTaskTabletSystem : EntitySystem
{
    [Dependency] private readonly AbductorTaskSystem _task = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

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
            args.Target is not {} target ||
            target == user || // lol no
            !HasComp<AbductorVictimComponent>(target)) // have to gizmo + abduct first chud
            return;

        args.Handled = true;
        if (_task.AllTasksCompleted(target))
        {
            _popup.PopupClient(Loc.GetString("abductor-task-tablet-already-completed"), user, user);
            return;
        }

        var netTarget = GetNetEntity(target);
        if (netTarget == ent.Comp.Target)
            return;

        ent.Comp.Target = netTarget;
        Dirty(ent);

        _popup.PopupClient(Loc.GetString("abductor-task-tablet-linked"), user, user);
        _ui.TryOpenUi(ent.Owner, AbductorTaskTabletUIKey.Key, user);
    }

    private void OnScan(Entity<AbductorTaskTabletComponent> ent, ref AbductorTaskScanMessage args)
    {
        if (GetEntity(ent.Comp.Target) is not {} target ||
            !InRange(ent, target) ||
            _task.IsSubject(target)) // no sound spamming
            return;

        EnsureComp<AbductorSubjectComponent>(target);
        _audio.PlayPredicted(ent.Comp.ScanSound, ent, args.Actor);
    }

    private void OnComplete(Entity<AbductorTaskTabletComponent> ent, ref AbductorTaskCompleteMessage args)
    {
        if (GetEntity(ent.Comp.Target) is not {} target || !InRange(ent, target))
            return;

        var user = args.Actor;
        if (!_task.TryCompleteTask(target))
        {
            _popup.PopupClient(Loc.GetString("abductor-task-tablet-incomplete"), user, user);
            return;
        }

        // TODO: objective for each completed task

        if (!_task.AllTasksCompleted(target))
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
