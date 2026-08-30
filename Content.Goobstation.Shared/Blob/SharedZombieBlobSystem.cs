// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Blob.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Goobstation.Shared.Blob;

public abstract partial class SharedZombieBlobSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityQuery<ZombieBlobComponent> _query = default!;

    [SubscribeLocalEvent(after: [typeof(SharedInteractionSystem)])]
    private void OnBUIMessageAttempt(Entity<ActivatableUIComponent> ent, ref BoundUserInterfaceMessageAttempt args)
    {
        if (args.Cancelled || !ent.Comp.RequiresComplex || !_query.HasComp(args.Actor))
            return;

        args.Cancel(); // no using computers and shit for blob zombies
    }

    [SubscribeLocalEvent]
    private void OnAttemptShoot(Entity<ZombieBlobComponent> ent, ref ShotAttemptedEvent args)
    {
        if (ent.Comp.CanShoot)
            return;

        _popup.PopupEntity("You can't use guns!", ent, ent);
        args.Cancel();
    }
}
