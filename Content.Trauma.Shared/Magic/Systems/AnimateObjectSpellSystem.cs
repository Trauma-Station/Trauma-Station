// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Wraith.WraithPoints;
using Content.Shared.Actions.Events;
using Content.Shared.Magic.Events;
using Content.Shared.Popups;
using Content.Trauma.Shared.Magic.Components;
using Content.Trauma.Shared.Magic.Events;

namespace Content.Trauma.Shared.Magic.Systems;

public sealed partial class AnimateObjectSpellSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnAnimateObjectAttempt(Entity<AnimateObjectActionComponent> ent, ref ActionAttemptEvent args)
    {
        if (args.Cancelled || args.Target is not { } target)
            return;

        if (TerminatingOrDeleted(target))
            return;

        if (!Transform(target).Anchored)
            return;

        args.Cancelled = true;
        _popup.PopupEntity("You can't animate an anchored object!", args.User, args.User);
    }

    [SubscribeLocalEvent]
    private void OnAnimateObjectSpell(AnimateObjectSpellEvent ev)
    {
        if (ev.Handled)
            return;

        if (TerminatingOrDeleted(ev.Target))
        {
            ev.Handled = true;
            return;
        }

        var componentEvent = new ChangeComponentsSpellEvent
        {
            Action = ev.Action,
            Performer = ev.Performer,
            Target = ev.Target,
            ToAdd = ev.ToAdd,
            ToRemove = ev.ToRemove,
        };

        ev.Handled = true;

        RaiseLocalEvent(componentEvent);
    }
}
