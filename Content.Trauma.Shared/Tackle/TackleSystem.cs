using Content.Shared.Buckle;
using Content.Shared.Damage.Systems;
using Content.Shared.Gravity;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Mobs.Components;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
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
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;


    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.Tackle, new PointerInputCmdHandler(HandleTackle))
            .Register<TackleSystem>();

        SubscribeLocalEvent<TacklingComponent, ThrowDoHitEvent>(OnHit);
        SubscribeLocalEvent<TacklingComponent, StopThrowEvent>(OnStopThrow);
        SubscribeLocalEvent<TacklingComponent, LandEvent>(OnLand);
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
        if (HasComp<MobStateComponent>(args.Target))
        {
            if (_standing.IsDown(args.Target))
                return;

            // Good

            return;
        }

        // Bad
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

        var ev = new TackleEvent(ent.Comp1.Range, ent.Comp1.Speed, ent.Comp1.MinDistance, ent.Comp1.KnockdownTime);

        if (!_stun.TryKnockdown(ent.Owner, ev.KnockdownTime, true, false))
            return false;

        dir *= ev.Range / len;

        var tackle = EnsureComp<TacklingComponent>(ent);
        tackle.MinDistance = ev.MinDistance;
        tackle.TackleStartPosition = start;

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
               !_buckle.IsBuckled(ent) &&
               !HasComp<StunnedComponent>(ent) && !HasComp<TacklingComponent>(ent) &&
               _hands.GetActiveItem(ent) == null && !_gravity.IsWeightless(ent);
    }
}
