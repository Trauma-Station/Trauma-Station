// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Medical.Common.Targeting;
using Content.Shared.ActionBlocker;
using Content.Shared.Buckle;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Trauma.Common.Input;
using Content.Trauma.Common.MartialArts;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Tackle;

public sealed partial class TackleSystem : EntitySystem
{
    [Dependency] private ISerializationManager _serMan = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedStaminaSystem _stam = default!;
    [Dependency] private SharedBuckleSystem _buckle = default!;
    [Dependency] private SharedGravitySystem _gravity = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private ThrownItemSystem _thrown = default!;
    [Dependency] private MobThresholdSystem _threshold = default!;
    [Dependency] private PullingSystem _pull = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private DamageableSystem _dmg = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(TraumaKeyFunctions.Tackle, new PointerInputCmdHandler(HandleTackle))
            .Register<TackleSystem>();

        SubscribeLocalEvent<TacklingComponent, ThrowDoHitEvent>(OnHit);
        SubscribeLocalEvent<TacklingComponent, StopThrowEvent>(OnStopThrow);
        SubscribeLocalEvent<TacklingComponent, LandEvent>(OnLand);

        SubscribeLocalEvent<TackleModifierComponent, BeingUnequippedAttemptEvent>(OnUnequipAttempt);

        Subs.SubscribeWithRelay<TackleModifierComponent, TackleEvent>(OnTackle, held: false);

        InitializeModifiers();
    }

    private void OnUnequipAttempt(Entity<TackleModifierComponent> ent, ref BeingUnequippedAttemptEvent args)
    {
        if (HasComp<TacklingComponent>(args.UnEquipTarget))
            args.Cancel();
    }

    private void OnTackle(Entity<TackleModifierComponent> ent, ref TackleEvent args)
    {
        foreach (var mod in ent.Comp.Modifiers)
        {
            if (mod.TackleCondition is { } condition &&
                !_conditions.TryCondition(ent, condition, args.User))
                continue;

            args.Sources.Add(mod);
        }
    }

    private void OnLand(Entity<TacklingComponent> ent, ref LandEvent args)
    {
        RemCompDeferred(ent, ent.Comp);
    }

    private void OnStopThrow(Entity<TacklingComponent> ent, ref StopThrowEvent args)
    {
        RemCompDeferred(ent, ent.Comp);
    }

    private void OnHit(Entity<TacklingComponent> ent, ref ThrowDoHitEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (!TryComp(ent, out TacklerComponent? tackler))
            return;

        if (ent.Comp.Source is not { } source)
            return;

        if (!source.AllowCollision)
            return;

        if (!TryComp(ent, out PhysicsComponent? body))
            return;

        var speed = body.LinearVelocity.Length() * source.SpeedModMultiplier * 0.4f;
        if (MathHelper.CloseToPercent(speed, 0f))
            return;

        var severity = 0f;

        var coords = GetCoordinates(ent.Comp.TackleStartPosition);
        var mapA = _xform.ToMapCoordinates(coords);
        var mapB = _xform.GetMapCoordinates(ent);
        if (mapA.MapId == mapB.MapId)
        {
            var distance = (mapA.Position - mapB.Position).Length();
            severity = (source.MinDistance - distance) * speed;
            severity = MathF.Max(0f, severity);
        }

        if (HasComp<MobStateComponent>(args.Target))
        {
            if (!HandleMobCollision((ent, ent.Comp, tackler), args.Target, source, speed))
                return;

            if (severity == 0f)
            {
                _thrown.StopThrow(ent, args.Component);
                return;
            }
        }

        if (ShouldStopTackle((ent.Owner, body), args.Target))
            severity += speed;

        if (severity == 0f)
            return;

        _thrown.StopThrow(ent, args.Component);

        severity *= source.SeverityModifier * 0.2f;

        _dmg.ChangeDamage(ent.Owner, tackler.BaseUserDamage * source.UserDamageMultiplier * severity, targetPart: TargetBodyPart.Head, canMiss: false);
        _stun.TryUpdateParalyzeDuration(ent.Owner, TimeSpan.FromSeconds(severity * (tackler.BaseUserKnockdownTime * source.KnockdownTimeMultiplier + 1f)));
    }

    private bool HandleMobCollision(Entity<TacklingComponent, TacklerComponent> user,
        EntityUid target,
        TackleModifier mod,
        float speed)
    {
        if (_standing.IsDown(target))
            return false;

        var ourMod = CalculateModifier(user, out _) + speed + mod.SkillMod;
        if (float.IsNaN(ourMod)) // curse of IEEE-754!
        {
            Log.Error($"Found NaN modifier for user {ToPrettyString(user)} with speed {speed} and mod {mod.SkillMod}!");
            return false;
        }

        var stamEv = new BeforeStaminaDamageEvent(1f);
        RaiseLocalEvent(target, ref stamEv);
        var stamResistMod = stamEv.Cancelled ? 1f : 1f - stamEv.Value;

        var theirMod = CalculateModifier(target, out var canTackle) + stamResistMod * mod.StamResistModifier * 4f;

        if (!canTackle)
            return true;

        if (float.IsNaN(theirMod)) // curse of IEEE-754!
        {
            Log.Error($"Found NaN modifier for target {ToPrettyString(target)} with stamres mods {stamResistMod} and {mod.StamResistModifier}!");
            return false;
        }

        const float a = 1.1f;

        var result = MathF.Pow(a, ourMod - theirMod);
        result = Math.Clamp(result, 0.2f, 5f);
        var invResult = 1f / result;

        var resultAdj = result - 0.5f;
        var invResultAdj = invResult - 0.5f;

        var userKnockdown = user.Comp2.BaseUserKnockdownTime * mod.UserKnockdownTimeMultiplier * invResultAdj * 0.5f;

        if (userKnockdown <= 0f)
            RemCompDeferred<KnockedDownComponent>(user);
        else
            _stun.UpdateKnockdownTime(user.Owner, TimeSpan.FromSeconds(userKnockdown));

        var targetKnockdown = TimeSpan.FromSeconds(user.Comp2.BaseTargetKnockdownTime * mod.TargetKnockdownTimeMultiplier * result);
        if (targetKnockdown > TimeSpan.Zero)
            _stun.TryKnockdown(target, targetKnockdown, drop: result > mod.DisarmThreshold * 1.5f, stunOnFail: false);

        if (resultAdj <= 0f)
            return true;

        if (mod.GrabOnSuccess)
            _pull.TryStartPull(user, target, grabStageOverride: GrabStage.Hard, force: true);

        var stamDamage = user.Comp2.BaseTargetStaminaDamage * mod.TargetStaminaDamageMultiplier * resultAdj;
        if (stamDamage > 0f)
            _stam.TakeStaminaDamage(target, stamDamage, source: user, ignoreResist: true);

        return true;
    }

    private float CalculateModifier(EntityUid uid, out bool canTackle)
    {
        var ev = new CalculateTackleModifierEvent(0f);
        RaiseLocalEvent(uid, ref ev);
        canTackle = ev.CanTackle;
        return ev.Modifier;
    }

    private bool ShouldStopTackle(Entity<PhysicsComponent?> user, Entity<FixturesComponent?> target)
    {
        if (!Resolve(user, ref user.Comp, false) || !Resolve(target, ref target.Comp, false))
            return false;

        foreach (var (_, fix) in target.Comp.Fixtures)
        {
            if (!fix.Hard)
                continue;

            if ((fix.CollisionLayer & user.Comp.CollisionMask) != 0)
                return true;
        }

        return false;
    }

    private bool HandleTackle(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        if (session?.AttachedEntity is not { } player || !Exists(player) || !coords.IsValid(EntityManager))
            return false;

        TryTackle(player, coords);

        return false;
    }

    public bool TryTackle(Entity<TacklerComponent?, TransformComponent?> ent, EntityCoordinates coords)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
            return false;

        if (!CanTackle(ent, ent.Comp1, ent.Comp2))
            return false;

        var start = _xform.GetMapCoordinates(ent, ent.Comp2);
        var end = _xform.ToMapCoordinates(coords);

        if (start.MapId != end.MapId)
            return false;

        var dir = end.Position - start.Position;
        var len = dir.Length();

        if (MathHelper.CloseToPercent(len, 0f))
            return false;

        var ev = new TackleEvent(ent, new());
        RaiseLocalEvent(ent, ref ev);

        if (ev.Sources.Count == 0)
            return false;

        var source = ev.Sources.Max();

        if (source?.Priority == null)
            return false;

        var copy = new TackleModifier();
        _serMan.CopyTo(source, ref copy, notNullableOverride: true);

        foreach (var s in ev.Sources)
        {
            if (s == source)
                continue;

            source.Modify(copy);
        }

        var modifier = CompOrNull<MovementSpeedModifierComponent>(ent)?.SprintSpeedModifier ?? 1f;

        var knockdownTime = ent.Comp1.KnockdownTime * copy.KnockdownTimeMultiplier;

        if (knockdownTime > TimeSpan.Zero && !_stun.TryKnockdown(ent.Owner, knockdownTime, true, false))
            return false;

        var staminaCost = ent.Comp1.StaminaCost * copy.StaminaCostMultiplier;

        if (staminaCost > 0f)
            _stam.TakeStaminaDamage(ent, staminaCost, ignoreResist: true);

        var range = ent.Comp1.Range * copy.RangeMultiplier * modifier;

        dir *= Math.Min(range, len) / len;

        var tackle = EnsureComp<TacklingComponent>(ent);
        tackle.TackleStartPosition = GetNetCoordinates(ent.Comp2.Coordinates);
        tackle.Source = copy;

        var cd = ent.Comp1.TackleCooldown * copy.CooldownMultiplier;

        ent.Comp1.NextTackle = _timing.CurTime + cd;

        Entity<TacklerComponent, TacklingComponent> dirty = (ent, ent.Comp1, tackle);
        Dirty(dirty);

        var speed = ent.Comp1.Speed * copy.SpeedMultiplier * modifier;

        _throwing.TryThrow(ent,
            dir,
            speed,
            ent,
            pushbackRatio: 0f,
            recoil: false,
            animated: false,
            doSpin: false);

        if (copy.UserEffect is { } effect)
            _effects.TryApplyEffect(ent, effect, user: ent);

        return true;
    }

    public bool CanTackle(EntityUid ent, TacklerComponent tackler, TransformComponent xform)
    {
        return _timing.CurTime >= tackler.NextTackle && !xform.Anchored && !_standing.IsDown(ent) &&
               !_buckle.IsBuckled(ent) && !HasComp<StunnedComponent>(ent) && !HasComp<TacklingComponent>(ent) &&
               !_gravity.IsWeightless(ent) && _blocker.CanInteract(ent, null) &&
               !_container.IsEntityOrParentInContainer(ent, xform: xform);
    }
}
