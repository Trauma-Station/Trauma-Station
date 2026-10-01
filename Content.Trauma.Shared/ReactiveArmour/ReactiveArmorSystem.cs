// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Timing;
using Content.Shared.Popups;
using Content.Shared.Emp;
using Content.Shared.Inventory;
using Content.Shared.EntityEffects;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Projectiles;
using Content.Trauma.Shared.Projectiles;

namespace Content.Trauma.Shared.ReactiveArmor;

public sealed partial class ReactiveArmorSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    // there has to be a better way to do this then making 3 methods for differen types of attacks... also do we even need a methot for hitscans?
    [SubscribeLocalEvent]
    private void OnHitMele(EntityUid uid, ReactiveArmorComponent comp, InventoryRelayedEvent<AttackedEvent> args)
    {
        CheckForCooldown(uid, comp, args.Owner);
    }

    [SubscribeLocalEvent]
    private void OnHitProjectile(EntityUid uid, ReactiveArmorComponent comp, InventoryRelayedEvent<GotHitByProjectileEvent> args)
    {
        CheckForCooldown(uid, comp, args.Owner);
    }

    [SubscribeLocalEvent]
    private void OnEmpPulse(EntityUid uid, ReactiveArmorComponent comp, EmpPulseEvent args)
    {
        comp.LastEmpd = _timing.CurTime;
        Dirty(uid, comp);
        /// How to make it work??
        // _popup.PopupEntity(Loc.GetString(comp.EmpMessage), args.Owner, args.Owner);

        // if (comp.ApplyOnEmpOnly){
        //     if (comp.ApplyEmpEffectOnUser) {
        //         _effects.ApplyEffects(args.Owner, comp.EmpEffects);
        //     }
        //     else {
        //         _effects.ApplyEffects(uid, comp.EmpEffects);
        //     }
        // }
    }

    private void CheckForCooldown(EntityUid uid, ReactiveArmorComponent comp, EntityUid target)
    {
        if (_timing.CurTime < comp.LastActivated + comp.ActivationDelay)
            return;

        comp.LastActivated = _timing.CurTime;
        Dirty(uid, comp);

        // apply emp or regular fx
        if (_timing.CurTime < comp.LastEmpd + comp.EmpDuration){
            if (comp.ApplyOnEmpOnly)
                return;

            if (comp.ApplyEmpEffectOnUser) {
                _effects.ApplyEffects(target, comp.EmpEffects);
            }
            else
                _effects.ApplyEffects(uid, comp.EmpEffects);
        }
        else
            _effects.ApplyEffects(target, comp.Effects);
    }
}
