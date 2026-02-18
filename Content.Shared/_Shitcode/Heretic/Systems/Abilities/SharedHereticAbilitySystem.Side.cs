using Content.Goobstation.Common.Weapons.DelayedKnockdown;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Heretic;
using Content.Shared.Projectiles;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;

namespace Content.Shared._Shitcode.Heretic.Systems.Abilities;

public abstract partial class SharedHereticAbilitySystem
{
    protected virtual void SubscribeSide()
    {
        SubscribeLocalEvent<EventHereticRustCharge>(OnRustCharge);
        SubscribeLocalEvent<EventHereticIceSpear>(OnIceSpear);
        SubscribeLocalEvent<EventHereticRealignment>(OnRealignment);
        SubscribeLocalEvent<EventEmp>(OnEmp);

        SubscribeLocalEvent<RealignmentComponent, StatusEffectEndedEvent>(OnStatusEnded);
        SubscribeLocalEvent<RealignmentComponent, BeforeStaminaDamageEvent>(OnBeforeRealignmentStamina);
    }

    private void OnStatusEnded(Entity<RealignmentComponent> ent, ref StatusEffectEndedEvent args)
    {
        if (args.Key != "Pacified")
            return;

        if (!StatusNew.TryRemoveStatusEffect(ent, ent.Comp.RealignmentStatus))
            RemCompDeferred(ent.Owner, ent.Comp);
    }

    private void OnRealignment(EventHereticRealignment args)
    {
        if (!TryUseAbility(args))
            return;

        var ent = args.Performer;

        StatusNew.TryRemoveStatusEffect(ent, args.StunStatus);
        StatusNew.TryRemoveStatusEffect(ent, args.DrowsinessStatus);
        StatusNew.TryRemoveStatusEffect(ent, args.SleepStatus);

        if (TryComp<StaminaComponent>(ent, out var stam))
        {
            if (stam.StaminaDamage >= stam.CritThreshold)
                _stam.ExitStamCrit(ent, stam);

            Dirty(ent, stam);
        }

        if (TryComp(ent, out CuffableComponent? cuffable) && _cuffs.TryGetLastCuff((ent, cuffable), out var cuffs))
            _cuffs.Uncuff(ent, null, cuffs.Value, cuffable);

        if (TryComp(ent, out EnsnareableComponent? ensnareable) && ensnareable.IsEnsnared &&
            ensnareable.Container.ContainedEntities.Count > 0)
        {
            var bola = ensnareable.Container.ContainedEntities[0];
            _snare.ForceFree(bola, Comp<EnsnaringComponent>(bola));
        }

        _pulling.StopAllPulls(ent, stopPuller: false);

        RemComp<KnockedDownComponent>(ent);
        RemCompDeferred<DelayedKnockdownComponent>(ent);

        if (Status.TryAddStatusEffect<PacifiedComponent>(ent, "Pacified", args.EffectTime, true))
            StatusNew.TryUpdateStatusEffectDuration(ent, args.RealignmentStatus, out _, args.EffectTime);
    }

    private void OnBeforeRealignmentStamina(Entity<RealignmentComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        if (args.Value <= 0)
            return;

        args.Cancelled = true;
    }

    private void OnEmp(EventEmp ev)
    {
        _emp.EmpPulse(Transform(ev.Performer).Coordinates, ev.Range, ev.EnergyConsumption, ev.Duration, ev.Performer);
        ev.Handled = true;
    }

    private void OnIceSpear(EventHereticIceSpear args)
    {
        if (!TryComp(args.Action, out IceSpearActionComponent? spearAction))
            return;

        if (!TryUseAbility(args, false))
            return;

        var ent = args.Performer;

        if (!TryComp(ent, out HandsComponent? hands))
            return;

        args.Handled = true;

        if (_net.IsClient)
            return;

        if (Exists(spearAction.CreatedSpear))
        {
            var spear = spearAction.CreatedSpear.Value;

            // TODO: When heretic spells are made the way wizard spell works don't handle this action if we can't pick it up.
            // It is handled now because it always speaks invocation no matter what.
            if (_hands.IsHolding((ent, hands), spear) || !_hands.TryGetEmptyHand((ent, hands), out var hand))
                return;

            if (TryComp(spear, out EmbeddableProjectileComponent? embeddable) && embeddable.EmbeddedIntoUid != null)
                _projectile.EmbedDetach(spear, embeddable);

            _transform.AttachToGridOrMap(spear);
            _transform.SetCoordinates(spear, Transform(ent).Coordinates);
            _hands.TryPickup(ent, spear, hand, false, handsComp: hands);
            return;
        }

        var newSpear = Spawn(spearAction.SpearProto, Transform(ent).Coordinates);
        if (!_hands.TryForcePickupAnyHand(ent, newSpear, false, hands))
        {
            QueueDel(newSpear);
            return;
        }

        spearAction.CreatedSpear = newSpear;
        EnsureComp<IceSpearComponent>(newSpear).ActionId = args.Action;
    }

    private void OnRustCharge(EventHereticRustCharge args)
    {
        if (!args.Target.IsValid(EntityManager) || !TryUseAbility(args))
            return;

        var ent = args.Performer;

        var xform = Transform(ent);

        if (!IsTileRust(xform.Coordinates, out _))
        {
            Popup.PopupClient(Loc.GetString("heretic-ability-fail-tile-underneath-not-rusted"), ent, ent);
            return;
        }

        var ourCoords = _transform.ToMapCoordinates(args.Target);
        var targetCoords = _transform.GetMapCoordinates(ent, xform);

        if (ourCoords.MapId != targetCoords.MapId)
            return;

        var dir = ourCoords.Position - targetCoords.Position;

        if (dir.LengthSquared() < 0.001f)
            return;

        RemComp<KnockedDownComponent>(ent);
        EnsureComp<RustChargeComponent>(ent);
        EnsureComp<RustObjectsInRadiusComponent>(ent);
        _throw.TryThrow(ent, dir.Normalized() * args.Distance, args.Speed, playSound: false, doSpin: false);

        args.Handled = true;
    }
}
