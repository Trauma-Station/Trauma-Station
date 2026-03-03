using Content.Goobstation.Common.MartialArts;
using Content.Medical.Common.DoAfter;
using Content.Medical.Common.Targeting;
using Content.Shared._Goobstation.Wizard.Mutate;
using Content.Shared.Buckle;
using Content.Shared.Clumsy;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Gravity;
using Content.Shared.Input;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Tackle;

public sealed class TackleSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedStaminaSystem _stam = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly ThrownItemSystem _thrown = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly PullingSystem _pull = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _dmg = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.Tackle, new PointerInputCmdHandler(HandleTackle))
            .Register<TackleSystem>();

        SubscribeLocalEvent<TacklingComponent, ThrowDoHitEvent>(OnHit);
        SubscribeLocalEvent<TacklingComponent, StopThrowEvent>(OnStopThrow);
        SubscribeLocalEvent<TacklingComponent, LandEvent>(OnLand);

        SubscribeLocalEvent<TackleModifierComponent, BeingUnequippedAttemptEvent>(OnUnequipAttempt);

        Subs.SubscribeWithRelay<TackleModifierComponent, TackleEvent>(OnTackle, held: false);
    }

    private void OnUnequipAttempt(Entity<TackleModifierComponent> ent, ref BeingUnequippedAttemptEvent args)
    {
        if (HasComp<TacklingComponent>(args.UnEquipTarget))
            args.Cancel();
    }

    private void OnTackle(Entity<TackleModifierComponent> ent, ref TackleEvent args)
    {
        if (args.Source != null && args.Source != args.User)
            return;

        args.Source = ent;
        args.Range *= ent.Comp.RangeMultiplier;
        args.Speed *= ent.Comp.SpeedMultiplier;
        args.KnockdownTime *= ent.Comp.KnockdownTimeMultiplier;
        args.StaminaCost *= ent.Comp.StaminaCostMultiplier;
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

        if (!Exists(ent.Comp.Source) || !TryComp(ent.Comp.Source, out TackleModifierComponent? mod))
            return;

        if (!TryComp(ent, out PhysicsComponent? body))
            return;

        var speed = body.LinearVelocity.Length() * mod.SpeedModMultiplier;
        if (MathHelper.CloseToPercent(speed, 0f))
            return;

        var severity = 0f;

        var coords = GetCoordinates(ent.Comp.TackleStartPosition);
        var mapA = _xform.ToMapCoordinates(coords);
        var mapB = _xform.GetMapCoordinates(ent);
        if (mapA.MapId == mapB.MapId)
        {
            var distance = (mapA.Position - mapB.Position).Length();
            severity = (mod.MinDistance - distance) * speed;
            switch (severity)
            {
                case < 0f:
                    severity = 0f;
                    break;
                case < 1f:
                    severity = 1f;
                    break;
            }
        }

        if (HasComp<MobStateComponent>(args.Target))
        {
            if (_standing.IsDown(args.Target))
                return;

            var ourMod = CalculateModifier((ent, body)) + speed + mod.SkillMod;

            var stamEv = new BeforeStaminaDamageEvent(1f);
            RaiseLocalEvent(args.Target, ref stamEv);
            var stamResistMod = stamEv.Cancelled ? 1f : 1f - stamEv.Value;

            var theirMod = CalculateModifier(args.Target) + stamResistMod;

            const float a = 1.1f;

            var result = MathF.Pow(a, ourMod) / MathF.Pow(a, theirMod);
            result = Math.Clamp(result, 0.2f, 5f);
            var invResult = 1f / result;

            var resultAdj = result - 0.5f;
            var invResultAdj = invResult - 0.5f;

            var userKnockdown = mod.BaseUserKnockdownTime * invResultAdj * 0.5f;

            if (userKnockdown <= 0f)
                RemCompDeferred<KnockedDownComponent>(ent);
            else
                _stun.UpdateKnockdownTime(ent.Owner, TimeSpan.FromSeconds(userKnockdown));

            var targetKnockdown = mod.BaseTargetKnockdownTime * result;
            _stun.TryKnockdown(args.Target, TimeSpan.FromSeconds(targetKnockdown), drop: result > 1f);

            if (resultAdj > 0f)
            {
                if (mod.GrabOnSuccess)
                    _pull.TryStartPull(ent, args.Target, grabStageOverride: GrabStage.Hard, force: true);

                var targetStun = mod.BaseTargetParalyzeTime * resultAdj;
                _stun.TryUpdateStunDuration(args.Target, TimeSpan.FromSeconds(targetStun));
                var stamDamage = mod.BaseTargetStaminaDamage * resultAdj;
                _stam.TakeStaminaDamage(args.Target, stamDamage, source: ent, ignoreResist: true);
            }

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

        severity *= mod.SeverityModifier;

        _dmg.ChangeDamage(ent.Owner, mod.BaseUserDamage * severity, targetPart: TargetBodyPart.Head, canMiss: false);
        _stun.TryUpdateParalyzeDuration(ent.Owner, TimeSpan.FromSeconds(severity * (mod.BaseUserKnockdownTime + 1f)));
    }

    private float CalculateModifier(Entity<PhysicsComponent?, StaminaComponent?, DamageableComponent?> ent)
    {
        var mod = 0f;

        if (Resolve(ent, ref ent.Comp1, false))
            mod += ent.Comp1.Mass / 140f - 0.5f;

        if (Resolve(ent, ref ent.Comp2, false))
            mod -= ent.Comp2.StaminaDamage / ent.Comp2.CritThreshold;

        if (Resolve(ent, ref ent.Comp3, false) &&
            (_threshold.TryGetThresholdForState(ent, MobState.SoftCrit, out var threshold) ||
             _threshold.TryGetThresholdForState(ent, MobState.Critical, out threshold) && threshold > 0f))
            mod -= ent.Comp3.TotalDamage.Value / threshold.Value.Float();

        if (HasComp<HulkComponent>(ent))
            mod += 2f;

        if (HasComp<ClumsyComponent>(ent))
            mod -= 2f;

        var doAfterModEv = new ModifyDoAfterDelayEvent();
        RaiseLocalEvent(ent, doAfterModEv);
        mod -= doAfterModEv.Multiplier - 1f;

        return mod;
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

        var ev = new TackleEvent(ent.Comp1.Range,
            ent.Comp1.Speed,
            ent.Comp1.StaminaCost,
            ent.Comp1.KnockdownTime,
            ent);

        RaiseLocalEvent(ent, ref ev);

        if (ev.Source is not { } source)
            return false;

        if (ev.KnockdownTime > TimeSpan.Zero && !_stun.TryKnockdown(ent.Owner, ev.KnockdownTime, true, false))
            return false;

        if (ev.StaminaCost > 0f)
            _stam.TakeStaminaDamage(ent, ev.StaminaCost, ignoreResist: true);

        dir *= ev.Range / len;

        var tackle = EnsureComp<TacklingComponent>(ent);
        tackle.TackleStartPosition = GetNetCoordinates(ent.Comp2.Coordinates);
        tackle.Source = source;

        ent.Comp1.NextTackle = _timing.CurTime + ent.Comp1.TackleCooldown;

        Entity<TacklerComponent, TacklingComponent> dirty = (ent, ent.Comp1, tackle);
        Dirty(dirty);

        Dirty(ent, ent.Comp1);

        _throwing.TryThrow(ent, dir, ev.Speed, ent, 0f, null, false, false, false, true, false);
        return true;
    }

    public bool CanTackle(EntityUid ent, TacklerComponent tackler, TransformComponent xform)
    {
        return _timing.CurTime >= tackler.NextTackle && !xform.Anchored && !_standing.IsDown(ent) &&
               !_buckle.IsBuckled(ent) && !HasComp<StunnedComponent>(ent) && !HasComp<TacklingComponent>(ent) &&
               !_gravity.IsWeightless(ent) && !_container.IsEntityOrParentInContainer(ent, xform: xform);
    }
}
