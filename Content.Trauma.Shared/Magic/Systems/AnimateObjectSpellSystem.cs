// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions.Events;
using Content.Shared.Popups;
using Content.Trauma.Shared.Magic.Components;

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
}

