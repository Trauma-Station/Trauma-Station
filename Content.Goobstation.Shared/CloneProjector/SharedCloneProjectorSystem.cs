// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.CloneProjector.Clone;
using Content.Goobstation.Shared.Holograms;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;

namespace Content.Goobstation.Shared.CloneProjector;

public abstract partial class SharedCloneProjectorSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    [SubscribeLocalEvent]
    private void OnStartup(Entity<HolographicCloneComponent> clone, ref ComponentStartup args)
    {
        EnsureComp<HologramVisualsComponent>(clone);
    }

    [SubscribeLocalEvent]
    private void OnMeleeHit(Entity<HolographicCloneComponent> clone, ref MeleeHitEvent args)
    {
        if (!args.IsHit
            || clone.Comp.HostEntity is not { } host)
            return;

        // Stop clones from punching their host.
        // Don't be a shitter.
        foreach (var hitEntity in args.HitEntities)
        {
            if (hitEntity != host
                || !_container.IsEntityOrParentInContainer(clone))
                continue;

            args.BonusDamage = -args.BaseDamage;
        }
    }

    [SubscribeLocalEvent]
    private void OnShotAttempted(Entity<HolographicCloneComponent> ent, ref ShotAttemptedEvent args)
    {
        if (ent.Comp.HostProjector is not { } hostProjector
            || !hostProjector.Comp.RestrictRangedWeapons)
            return;

        _popup.PopupEntity(Loc.GetString("gun-disabled"), ent, ent);
        args.Cancel();
    }
}
