// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Wraith;
using Content.Goobstation.Shared.Wraith.Events;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Popups;
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
    [Dependency] private SharedPopupSystem _popup = default!;

    private const int MaxCursesBeforeFinal = 4;

    [SubscribeLocalEvent]
    private void OnApplyCurseAction(ApplyCurseActionEvent args)
    {
        var curse = args.Curse;
        var user = args.Performer;
        var attemptEv = new CurseAttemptEvent(user);
        RaiseLocalEvent(args.Target, ref attemptEv);

        if (attemptEv.Cancelled)
            return;

        // Add the curseHolder component and the new curse on the target
        var curseHolder = EnsureComp<CurseHolderComponent>(args.Target);

        if (args.RequireAllCurses)
        {
            if (curseHolder.ActiveCurses.Count < MaxCursesBeforeFinal)
            {
                _popup.PopupEntity(Loc.GetString("curse-fail-require-all"), user, user);
                return;
            }
        }

        var curseApply = new CurseAppliedEvent(curse, user);
        RaiseLocalEvent(args.Target, ref curseApply);

        if (curseApply.Cancelled)
            return;

        if (args.Popup.HasValue)
            _popup.PopupEntity(Loc.GetString(args.Popup.Value), user, user, PopupType.Medium);

        // play curse sound if it exists
        if (args.CurseSound != null)
            _audio.PlayPredicted(args.CurseSound, args.Target, user);

        // Reset timers on all curses for the user
        if (!TryComp<ActionsComponent>(user, out var actions))
            return;

        foreach (var action in actions.Actions)
        {
            if (!HasComp<CurseActionComponent>(action))
                continue;

            _actions.StartUseDelay(action);
        }

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnImmuneCurseAttempt(Entity<CurseImmuneComponent> ent, ref CurseAttemptEvent args)
    {
        _popup.PopupEntity(Loc.GetString("curse-immune-fail"), args.Curser, args.Curser);
        args.Cancelled = true;
    }
}
