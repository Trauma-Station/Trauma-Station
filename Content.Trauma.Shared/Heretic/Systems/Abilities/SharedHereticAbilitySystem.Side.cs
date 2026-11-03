// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Weapons.DelayedKnockdown;
using Content.Medical.Common.Targeting;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body.Components;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.DoAfter;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Trauma.Common.CollectiveMind;
using Content.Trauma.Shared.Heretic.Components.Side;
using Content.Trauma.Shared.Heretic.Components.StatusEffects;
using Content.Trauma.Shared.Heretic.Events;

namespace Content.Trauma.Shared.Heretic.Systems.Abilities;

public abstract partial class SharedHereticAbilitySystem
{
    [Dependency] private EntityQuery<BloodstreamComponent> _bloodQuery = default!;

    private readonly List<EntityUid> _bloodStealTargets = new();
    private static readonly ProtoId<StatusEffectPrototype> Pacified = "Pacified";

    [SubscribeLocalEvent]
    private void OnCloak(EventHereticCloak args)
    {
        var ent = args.Performer;

        if (StatusNew.TryEffectsWithComp<HereticCloakedStatusEffectComponent>(ent, out var effects))
        {
            foreach (var effect in effects)
            {
                PredictedQueueDel(effect.Owner);
            }

            args.Handled = true;
            return;
        }

        // TryUseAbility only if we are not cloaked so that we can uncloak without focus
        if (!TryUseAbility(args))
            return;

        StatusNew.TryAddStatusEffect(ent, args.Status, out _, args.Lifetime);
    }

    [SubscribeLocalEvent]
    private void OnStatusEnded(Entity<RealignmentComponent> ent, ref StatusEffectEndedEvent args)
    {
        if (args.Key != Pacified)
            return;

        if (!StatusNew.TryRemoveStatusEffect(ent, ent.Comp.RealignmentStatus))
            RemCompDeferred(ent.Owner, ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnRealignment(EventHereticRealignment args)
    {
        if (!TryUseAbility(args))
            return;

        var ent = args.Performer;

        foreach (var effect in args.RemovedEffects)
        {
            StatusNew.TryRemoveStatusEffect(ent, effect);
        }

        if (TryComp<StaminaComponent>(ent, out var stam))
        {
            if (stam.StaminaDamage >= stam.CritThreshold)
                _stam.ExitStamCrit(ent, stam);

            Dirty(ent, stam);
        }

        if (TryComp(ent, out CuffableComponent? cuffable) && _cuffs.TryGetLastCuff((ent, cuffable), out var cuffs))
            _cuffs.Uncuff(ent, null, cuffs.Value, cuffable);

        if (TryComp(ent, out EnsnareableComponent? ensnareable) && _snare.IsEnsnared((ent, ensnareable)))
        {
            var bola = ensnareable.Container!.ContainedEntities[0];
            _snare.ForceFree(bola);
        }

        _pulling.StopAllPulls(ent, stopPuller: false);

        RemComp<KnockedDownComponent>(ent);
        RemCompDeferred<DelayedKnockdownComponent>(ent);

        if (Status.TryAddStatusEffect<PacifiedComponent>(ent, "Pacified", args.EffectTime, true))
            StatusNew.TryUpdateStatusEffectDuration(ent, args.RealignmentStatus, out _, args.EffectTime);
    }

    [SubscribeLocalEvent]
    private void OnBeforeRealignmentStamina(Entity<RealignmentComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        if (args.Value <= 0)
            return;

        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnEmp(EventEmp ev)
    {
        _emp.EmpPulse(Transform(ev.Performer).Coordinates, ev.Range, ev.EnergyConsumption, ev.Duration, ev.Performer);
        ev.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnCleave(EventHereticCleave args)
    {
        if (!TryUseAbility(args, false))
            return;

        if (!args.Target.IsValid())
            return;

        var coords = Transform(args.Target).Coordinates;

        if (!Examine.InRangeUnOccluded(args.Performer, coords))
        {
            Popup.PopupEntity(Loc.GetString("heretic-ability-fail-cant-see"), args.Performer, args.Performer);
            return;
        }

        args.Handled = true;

        PredictedSpawnAtPosition(args.Effect, coords);

        var hasTargets = false;

        TryComp(args.Performer, out DamageableComponent? damageable);

        _bloodStealTargets.Clear();
        foreach (var (target, _) in GetNearbyPeople(args.Performer, args.Range, null, coords))
        {
            if (target == args.Performer)
                continue;

            hasTargets = true;

            var dmg = _dmg.ChangeDamage(target, args.Damage, true, origin: args.Performer, targetPart: TargetBodyPart.Vital);
            if (damageable != null)
                _lifesteal.LifeSteal((args.Performer, damageable), dmg.GetTotal());

            if (!_bloodQuery.TryComp(target, out var blood))
                continue;

            _blood.TryModifyBleedAmount((target, blood), blood.MaxBleedAmount);
            _bloodStealTargets.Add(target);
        }

        if (!hasTargets)
            return;

        _lifesteal.BloodSteal(args.Performer, _bloodStealTargets, args.BloodModifyAmount, null);
        _audio.PlayPredicted(args.Sound, args.Target, args.Performer);

        // Prevent WoundSystem from throwing on client
        if (_net.IsClient)
            return;

        foreach (var (_, woundable) in _body.GetOrgans<WoundableComponent>(args.Performer))
        {
            _container.EmptyContainer(woundable.Wounds);
        }
    }

    [SubscribeLocalEvent]
    private void OnMansusLink(EventHereticMansusLink args)
    {
        if (!TryUseAbility(args, false))
            return;

        var ent = args.Performer;
        var target = args.Target;
        if (!HasComp<MindContainerComponent>(target))
        {
            Popup.PopupEntity(Loc.GetString("heretic-manselink-fail-nomind"), ent, ent);
            return;
        }

        if (TryComp<CollectiveMindComponent>(target, out var mind) && mind.Channels.Contains(MansusLinkMind))
        {
            Popup.PopupEntity(Loc.GetString("heretic-manselink-fail-exists"), ent, ent);
            return;
        }

        args.Handled = true;

        var dargs = new DoAfterArgs(EntityManager,
            ent,
            5f,
            new HereticMansusLinkDoAfter(),
            eventTarget: target,
            target: target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            MultiplyDelay = false
        };

        Popup.PopupEntity(Loc.GetString("heretic-manselink-start"), ent, ent);
        Popup.PopupEntity(Loc.GetString("heretic-manselink-start-target"), target, target, PopupType.MediumCaution);
        DoAfter.TryStartDoAfter(dargs);
    }

    [SubscribeLocalEvent]
    private void OnMansusLinkDoafter(Entity<MindContainerComponent> ent, ref HereticMansusLinkDoAfter args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        EnsureComp<CollectiveMindComponent>(ent).Channels.Add(MansusLinkMind);

        _flash.Flash(ent,
            null,
            null,
            TimeSpan.FromSeconds(2f),
            0f,
            false,
            true,
            stunDuration: TimeSpan.FromSeconds(1f));
    }
}
