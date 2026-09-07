// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Timing;
using Content.Shared.Inventory;
using Content.Shared.EntityEffects;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Projectiles;
using Content.Trauma.Shared.Projectiles;

namespace Content.Trauma.Shared.ReactiveArmour;

public sealed partial class ReactiveArmourSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    // there has to be a better way to do this then making 3 methods for differen types of attacks... also do we even need a methot for hitscans?
    [ SubscribeLocalEvent ]
    private void OnHitMele(EntityUid uid, ReactiveArmourComponent comp, InventoryRelayedEvent<AttackedEvent> args)
    {
        CheckForCooldown(uid, comp, args.Owner);
    }

    [ SubscribeLocalEvent ]
    private void OnHitProjectile(EntityUid uid, ReactiveArmourComponent comp, InventoryRelayedEvent<GotHitByProjectileEvent> args)
    {
        CheckForCooldown(uid, comp, args.Owner);
    }

    private void CheckForCooldown(EntityUid uid, ReactiveArmourComponent comp, EntityUid target)
    {
        if (_timing.CurTime < comp.LastActivated + comp.ActivationDelay)
            return;

        comp.LastActivated = _timing.CurTime;
        Dirty(uid, comp);

        _effects.ApplyEffects(target, comp.Effects);
    }
}
