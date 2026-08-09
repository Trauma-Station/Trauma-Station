// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Stunnable;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Emoting;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Speech;
using Content.Shared.Random.Helpers;
using Content.Shared.Standing;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Temperature;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Content.Trauma.Common.Interaction;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Wizard.Traps;

public sealed partial class IceCubeSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private FixtureSystem _fixtures = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedTemperatureSystem _temp = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityQuery<TemperatureComponent> _tempQuery = default!;

    /// <summary>
    /// Damage types that can break ice cubes.
    /// </summary>
    private static readonly HashSet<ProtoId<DamageTypePrototype>> BreakDamages = new() { "Blunt", "Slash", "Piercing", "Ballistic", "Heat" };
    private static readonly ProtoId<DamageTypePrototype> Heat = "Heat";
    public static readonly EntProtoId StatusEffectStunned = "StatusEffectStunned";

    private const string IceCubeFixture = "ice-cube-fixture";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IceCubeComponent, UseAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<IceCubeComponent, PickupAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<IceCubeComponent, ThrowAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<IceCubeComponent, AttackAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<IceCubeComponent, EmoteAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<IceCubeComponent, SpeakAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<IceCubeComponent, StandAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<IceCubeComponent, DownAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<IceCubeComponent, ChangeDirectionAttemptEvent>(OnAttempt);
    }

    [SubscribeLocalEvent]
    private void OnCanBeInteractedWith(Entity<IceCubeComponent> ent, ref CanBeInteractedWithEvent args)
    {
        // This prevents cuffs/hyposprays/etc but allows pulls
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnStatus(Entity<IceCubeComponent> ent, ref BeforeStatusEffectAddedEvent args)
    {
        if (args.Effect == StatusEffectStunned)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnKnockDown(Entity<IceCubeComponent> ent, ref KnockDownAttemptEvent args)
    {
        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnStamina(Entity<IceCubeComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnModifyDamage(Entity<IceCubeComponent> ent, ref DamageModifyEvent args)
    {
        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, ent.Comp.DamageReduction);
    }

    [SubscribeLocalEvent]
    private void OnStartCollide(Entity<IceCubeComponent> ent, ref StartCollideEvent args)
    {
        var lenSquared = args.OtherBody.LinearVelocity.LengthSquared();
        if (lenSquared < 0.01f ||
            !lenSquared.IsValid()) // Tests heisenfail without this since an engine issue causes it to return NaN randomly
            return;

        var xform = Transform(args.OtherEntity);

        var ray = new CollisionRay(_transform.GetWorldPosition(xform),
            args.OtherBody.LinearVelocity.Normalized(),
            args.OurBody.CollisionLayer);

        if (ent.Owner != _physics.IntersectRay(xform.MapID, ray, 1f, args.OtherEntity).FirstOrNull()?.HitEntity)
            return;

        _physics.ApplyLinearImpulse(ent,
            args.OtherBody.LinearVelocity * args.OtherBody.Mass * ent.Comp.VelocityMultiplier,
            body: args.OurBody);
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<IceCubeComponent> ent, ref MobStateChangedEvent args)
    {
        RemCompDeferred(ent.Owner, ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnTileFriction(Entity<IceCubeComponent> ent, ref TileFrictionEvent args)
    {
        args.Modifier *= ent.Comp.TileFriction;
    }

    [SubscribeLocalEvent]
    private void OnBreakFree(Entity<IceCubeComponent> ent, ref BreakFreeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        RemCompDeferred(ent.Owner, ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnMoveInput(Entity<IceCubeComponent> ent, ref MoveInputEvent args)
    {
        var (uid, comp) = ent;

        var doArgs = new DoAfterArgs(EntityManager, uid, comp.BreakFreeDelay, new BreakFreeDoAfterEvent(), uid)
        {
            Hidden = true,
            RequireCanInteract = false,
            MultiplyDelay = false,
            CancelDuplicate = false,
        };

        if (_doAfter.TryStartDoAfter(doArgs))
            _popup.PopupEntity(Loc.GetString("ice-cube-break-free-start"), uid, uid);
    }

    [SubscribeLocalEvent]
    private void OnInteractAttempt(Entity<IceCubeComponent> ent, ref InteractionAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnAttempt(EntityUid uid, IceCubeComponent component, CancellableEntityEventArgs args)
    {
        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnPullAttempt(Entity<IceCubeComponent> ent, ref PullAttemptEvent args)
    {
        if (args.PullerUid == ent.Owner)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnUpdateCanMove(Entity<IceCubeComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (ent.Comp.LifeStage > ComponentLifeStage.Running)
            return;

        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnHit(Entity<IceCubeOnProjectileHitComponent> ent, ref ProjectileHitEvent args)
    {
        if (_whitelist.IsValid(ent.Comp.Whitelist, args.Target))
            EnsureComp<IceCubeComponent>(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnDamageDealt(Entity<IceCubeComponent> ent, ref DamageDealtEvent args)
    {
        var (uid, comp) = ent;

        if (!_tempQuery.TryComp(uid, out var temp))
            return;

        if (!args.Damage.AnyPositive())
            return;

        if (args.Damage.DamageDict.TryGetValue(Heat, out var heat))
        {
            // thermodymagics
            _temp.SetTemperature((uid, temp),
                MathF.Min(comp.UnfreezeTemperatureThreshold + 10f,
                    temp.Temperature + heat.Float() * comp.TemperaturePerHeatDamageIncrease));
        }

        var total = FixedPoint2.Zero;
        foreach (var (type, value) in args.Damage.DamageDict)
        {
            if (BreakDamages.Contains(type))
                total += value;
        }

        if (total <= FixedPoint2.Zero)
            return;

        ent.Comp.SustainedDamage += total.Float() * ent.Comp.SustainedDamageMeltProbabilityMultiplier;

        if (ShouldUnfreeze(ent, temp.Temperature))
            RemCompDeferred(ent, ent.Comp);
        else
            Dirty(ent);
    }

    private bool ShouldUnfreeze(Entity<IceCubeComponent> ent, float curTemp)
    {
        if (ent.Comp.SustainedDamage <= ent.Comp.DamageMeltProbabilityThreshold)
            return false;

        var damage = ent.Comp.SustainedDamage * 0.01f; // damage part guaranteed at 100
        var tempCurve = InverseLerp(ent.Comp.FrozenTemperature, ent.Comp.UnfrozenTemperature, curTemp); // temp part guaranteed at unfrozen temp
        var chance = Math.Clamp(damage * tempCurve, 0.2f, 1f);

        return SharedRandomExtensions.PredictedProb(_timing, chance, GetNetEntity(ent));
    }

    private float InverseLerp(float min, float max, float value)
        => max <= min ? 1f : Math.Clamp((value - min) / (max - min), 0f, 1f);

    [SubscribeLocalEvent]
    private void OnTemperatureChanged(Entity<IceCubeComponent> ent, ref TemperatureChangedEvent args)
    {
        if (args.CurrentTemperature > args.LastTemperature && args.CurrentTemperature > ent.Comp.UnfreezeTemperatureThreshold)
            RemCompDeferred(ent, ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnRemove(Entity<IceCubeComponent> ent, ref ComponentRemove args)
    {
        var (uid, comp) = ent;

        if (TerminatingOrDeleted(uid))
            return;

        if (_tempQuery.TryComp(uid, out var temp))
        {
            _temp.SetTemperature((uid, temp), MathF.Max(temp.Temperature, comp.UnfrozenTemperature));
        }

        _blocker.UpdateCanMove(uid);

        _popup.PopupEntity(Loc.GetString("ice-cube-melt"), uid);

        if (!TryComp(uid, out PhysicsComponent? physics) || !TryComp(uid, out FixturesComponent? fixtures))
            return;

        var xform = Transform(uid);

        var fixture = _fixtures.GetFixtureOrNull(uid, IceCubeFixture, fixtures);

        if (fixture != null)
            _fixtures.DestroyFixture(uid, IceCubeFixture, fixture, body: physics, manager: fixtures, xform: xform);
        else
            _fixtures.FixtureUpdate(uid, manager: fixtures, body: physics);

        if (comp.OldBodyType != null)
            _physics.SetBodyType(uid, comp.OldBodyType.Value, fixtures, physics, xform);
    }

    [SubscribeLocalEvent]
    private void OnInit(Entity<IceCubeComponent> ent, ref ComponentInit args)
    {
        var (uid, comp) = ent;

        if (_tempQuery.TryComp(uid, out var temp))
        {
            _temp.SetTemperature((uid, temp), MathF.Min(temp.Temperature, comp.FrozenTemperature));
        }

        _blocker.UpdateCanMove(uid);

        if (!TryComp(uid, out PhysicsComponent? physics) || !TryComp(uid, out FixturesComponent? fixtures))
            return;

        var xform = Transform(uid);

        // TODO: shitcode alert
        // For whatever reason I can't set bounds on PhysShapeAabb in code so I have to use polygon shape
        var shape = new PolygonShape();
        shape.SetAsBox(new Box2(-0.4f, -0.4f, 0.4f, 0.4f));
        _fixtures.TryCreateFixture(uid,
            shape,
            IceCubeFixture,
            collisionLayer: comp.CollisionLayer,
            collisionMask: comp.CollisionMask,
            restitution: comp.Restitution,
            manager: fixtures,
            body: physics,
            xform: xform);

        if (physics.BodyType != BodyType.KinematicController)
            return;

        comp.OldBodyType = physics.BodyType;
        Dirty(ent);
        _physics.SetBodyType(uid, comp.FrozenBodyType, fixtures, physics, xform);
    }
}

[Serializable, NetSerializable]
public sealed partial class BreakFreeDoAfterEvent : SimpleDoAfterEvent;
