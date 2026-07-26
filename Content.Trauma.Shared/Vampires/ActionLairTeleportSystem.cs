// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Storage.EntitySystems;
using Content.Trauma.Shared.Teleportation;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.Vampires;

public sealed partial class ActionLairTeleportSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private TeleportSystem _teleport = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedEntityStorageSystem _storage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionLairTeleportComponent, ActionLairTeleportEvent>(OnAction);
    }

    private void OnAction(Entity<ActionLairTeleportComponent> ent, ref ActionLairTeleportEvent args)
    {
        var user = args.Performer;

        if (TerminatingOrDeleted(ent.Comp.Lair) || ent.Comp.Lair is not { } lair)
        {
            _popup.PopupEntity("You do not have a lair anymore!", user, user, PopupType.MediumCaution);
            return;
        }

        if (_mob.IsAlive(user))
        {
            _popup.PopupEntity("You can not teleport to your lair while alive!", user, user, PopupType.MediumCaution);
            return;
        }

        _teleport.Teleport(
            uid: user,
            coords: Transform(lair).Coordinates,
            user: user);

        _container.TryGetContainer(lair, ent.Comp.StorageId, out var storage);
        if (storage is { } store)
        {
            _storage.TryCloseStorage(lair, user);
            _container.Insert(user, store);
        }

        args.Handled = true;
    }

    /// <summary>
    /// Sets the lair that was created by <see cref="ActionLairComponent"/> action, on this action entity.
    /// </summary>
    public void SetLair(Entity<ActionLairTeleportComponent?> ent, EntityUid lair)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.Lair = lair;
        Dirty(ent);
    }
}
