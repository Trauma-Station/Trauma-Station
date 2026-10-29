// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Emp;
using Content.Shared.EntityEffects;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Projectiles;
using Content.Trauma.Shared.Projectiles;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.ReactiveArmor;

public sealed partial class ReactiveArmorSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnHitMele(Entity<ReactiveArmorComponent> ent, ref InventoryRelayedEvent<AttackedEvent> args)
    {
        CheckForCooldown(ent, args.Owner);
    }

    [SubscribeLocalEvent]
    private void OnHitProjectile(Entity<ReactiveArmorComponent> ent, ref InventoryRelayedEvent<GotHitByProjectileEvent> args)
    {
        CheckForCooldown(ent, args.Owner);
    }

    [SubscribeLocalEvent]
    private void OnEmpPulse(Entity<ReactiveArmorComponent> ent, ref EmpPulseEvent args)
    {
        ent.Comp.LastEmpd = _timing.CurTime;
        Dirty(ent.Owner, ent.Comp);

        var userUid = Transform(ent.Owner).ParentUid;
        _popup.PopupEntity(Loc.GetString(ent.Comp.EmpMessage), userUid, userUid);
    }

    private void CheckForCooldown(Entity<ReactiveArmorComponent> ent, EntityUid user)
    {
        if (_timing.CurTime < ent.Comp.LastActivated + ent.Comp.ActivationDelay)
            return;

        ent.Comp.LastActivated = _timing.CurTime;
        Dirty(ent.Owner, ent.Comp);

        var target = user;
        var effects = ent.Comp.Effects;

        if (_timing.CurTime < ent.Comp.LastEmpd + ent.Comp.EmpDuration)
        {
            effects = ent.Comp.EmpEffects;
            if (!ent.Comp.ApplyEmpEffectOnUser)
                target = ent.Owner;
        }

        _effects.ApplyEffects(target, effects);
    }
}
