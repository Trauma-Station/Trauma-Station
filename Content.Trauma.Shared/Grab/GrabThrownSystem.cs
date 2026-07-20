// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Effects;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Trauma.Common.Grab;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;

namespace Content.Trauma.Shared.Grab;

public sealed partial class GrabThrownSystem : CommonGrabThrownSystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedColorFlashEffectSystem _color = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private INetManager _netMan = default!;
    [Dependency] private SharedStunSystem _stun = default!;

    public const float MinMass = 30f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrabThrownComponent, StartCollideEvent>(HandleCollide);
        SubscribeLocalEvent<GrabThrownComponent, StopThrowEvent>(OnStopThrow);
    }

    public override bool IsGrabThrown(EntityUid thrown)
    {
        return HasComp<GrabThrownComponent>(thrown);
    }

    private void HandleCollide(Entity<GrabThrownComponent> ent, ref StartCollideEvent args)
    {
        if (_netMan.IsClient) // To avoid effect spam
            return;

        if (!HasComp<ThrownItemComponent>(ent))
        {
            RemComp<GrabThrownComponent>(ent);
            return;
        }

        if (ent.Comp.IgnoreEntity.Contains(args.OtherEntity))
            return;

        if (!HasComp<DamageableComponent>(ent))
            RemComp<GrabThrownComponent>(ent);

        if (!TryComp<PhysicsComponent>(ent, out var physicsComponent))
            return;

        ent.Comp.IgnoreEntity.Add(args.OtherEntity);

        var velocitySquared = args.OurBody.LinearVelocity.LengthSquared();
        var mass = physicsComponent.Mass;
        if (mass < MinMass)
            return; // don't care about mice and stuff

        var kineticEnergy = 0.5f * mass * velocitySquared;
        var kineticEnergyDamage = new DamageSpecifier();
        kineticEnergyDamage.DamageDict.Add("Blunt", 1);
        var modNumber = Math.Floor(kineticEnergy / 100);
        kineticEnergyDamage *= Math.Floor(modNumber / 3);
        _damageable.TryChangeDamage(args.OtherEntity, kineticEnergyDamage);
        _stamina.TakeStaminaDamage(ent, (float) Math.Floor(modNumber / 2));

        _stun.TryCrawling(args.OtherEntity);

        _color.RaiseEffect(Color.Red, new List<EntityUid>() { ent }, Filter.Pvs(ent, entityManager: EntityManager));
    }

    private void OnStopThrow(EntityUid uid, GrabThrownComponent comp, StopThrowEvent args)
    {
        if (comp.DamageOnCollide != null)
            _damageable.TryChangeDamage(uid, comp.DamageOnCollide);

        RemCompDeferred(uid, comp);
    }

    public override void Throw(
        EntityUid uid,
        EntityUid thrower,
        Vector2 vector,
        float grabThrownSpeed,
        DamageSpecifier? damage = null,
        bool drop = true)
    {
        var comp = EnsureComp<GrabThrownComponent>(uid);
        comp.IgnoreEntity.Add(thrower);
        comp.DamageOnCollide = damage;

        _stun.TryCrawling(uid, drop: drop);
        _throwing.TryThrow(uid, vector, grabThrownSpeed, animated: false);
    }
}
