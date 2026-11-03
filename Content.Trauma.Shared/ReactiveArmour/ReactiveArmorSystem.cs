// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Emp;
using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Trauma.Common.Projectiles;
using Content.Trauma.Common.Weapons;
using Content.Trauma.Shared.EntityEffects;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.ReactiveArmor;

public sealed partial class ReactiveArmorSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EffectDataSystem _data = default!;

    [SubscribeLocalEvent]
    private void OnHitMele(Entity<ReactiveArmorComponent> ent, ref InventoryRelayedEvent<BeforeHarmfulActionEvent> args)
    {
        if (args.Args.Cancelled || !args.Args.CanRiposte || args.Args.Type != HarmfulActionType.Harm)
            return;

        if (TryBlock(ent, args.Args.Target, args.Args.User))
            args.Args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnHitProjectile(Entity<ReactiveArmorComponent> ent, ref InventoryRelayedEvent<GotHitByProjectileEvent> args)
    {
        if (args.Args.Cancelled)
            return;

        if (TryBlock(ent, args.Owner, args.Args.Shooter))
            args.Args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnEmpPulse(Entity<ReactiveArmorComponent> ent, ref EmpPulseEvent args)
    {
        ent.Comp.LastEmpd = _timing.CurTime;
        Dirty(ent.Owner, ent.Comp);

        var userUid = Transform(ent.Owner).ParentUid;
        _popup.PopupEntity(Loc.GetString(ent.Comp.EmpMessage), userUid, userUid);
    }

    private bool TryBlock(Entity<ReactiveArmorComponent> ent, EntityUid target, EntityUid? user)
    {
        var now = _timing.CurTime;

        if (now < ent.Comp.LastActivated + ent.Comp.ActivationDelay)
            return false;

        if (!_conditions.TryConditions(target, ent.Comp.TargetConditions))
            return false;

        ent.Comp.LastActivated = now;
        Dirty(ent.Owner, ent.Comp);

        var effects = ent.Comp.Effects;
        var result = true;

        if (now < ent.Comp.LastEmpd + ent.Comp.EmpDuration)
        {
            effects = ent.Comp.EmpEffects;
            result = false;
            if (!ent.Comp.ApplyEmpEffectOnUser)
                target = ent.Owner;
        }

        _data.SetTool(target, ent.Owner);
        _effects.ApplyEffects(target, effects, user: user);
        _data.ClearTool(target);
        return result;
    }
}
