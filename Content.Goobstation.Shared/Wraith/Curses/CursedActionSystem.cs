// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Wraith.Events;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Popups;
using Content.Trauma.Common.Silicon;
using Robust.Shared.Audio.Systems;

namespace Content.Goobstation.Shared.Wraith.Curses;

/// <summary>
/// This handles applying curses to an entity.
/// This system also handles entities that are not allowed to get curses
/// </summary>
public sealed partial class CursedActionSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private CommonSiliconSystem _silicon = default!;

    private const int MaxCursesBeforeFinal = 4;

    [SubscribeLocalEvent]
    private void OnApplyCurseAction(ApplyCurseActionEvent args)
    {
        if (args.Curse == null)
            return;

        var attemptEv = new AttemptCurseEvent(args.Target, args.Performer);
        RaiseLocalEvent(args.Target, ref attemptEv);

        if (attemptEv.Cancelled)
            return;

        // Add the curseHolder component and the new curse on the target
        var curseHolder = EnsureComp<CurseHolderComponent>(args.Target);

        if (args.RequireAllCurses)
        {
            if (curseHolder.ActiveCurses.Count < MaxCursesBeforeFinal)
            {
                _popup.PopupEntity(Loc.GetString("curse-fail-require-all"), args.Performer, args.Performer);
                return;
            }
        }

        var curseApply = new CurseAppliedEvent(args.Curse.Value, args.Performer);
        RaiseLocalEvent(args.Target, ref curseApply);

        if (curseApply.Cancelled)
            return;

        if (args.Popup.HasValue)
            _popup.PopupEntity(Loc.GetString(args.Popup.Value), args.Performer, args.Performer, PopupType.Medium);

        // play curse sound if it exists
        if (args.CurseSound != null && _net.IsServer)
            _audio.PlayEntity(args.CurseSound, args.Target, args.Target);

        // Reset timers on all curses for the user
        if (!TryComp<ActionsComponent>(args.Performer, out var actions))
            return;

        foreach (var action in actions.Actions)
        {
            if (!HasComp<CurseActionComponent>(action))
                continue;

            _actions.StartUseDelay(action);
        }

        args.Handled = true;
    }

    #region Cancel Events
    [SubscribeLocalEvent]
    private void OnSiliconAttempt(ref AttemptCurseEvent args)
    {
        if (_silicon.IsSilicon(args.Entity))
            _popup.PopupEntity(Loc.GetString("curse-fail-robot"), args.Curser, args.Curser);
        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnAttemptCurseImmune(Entity<CurseImmuneComponent> ent, ref AttemptCurseEvent args)
    {
        _popup.PopupEntity(Loc.GetString("curse-immune-fail"), args.Curser, args.Curser);
        args.Cancelled = true;
    }
    #endregion
}
