using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared._DV.CosmicCult.Components;
using Content.Shared.Audio;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Maps;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Warps;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._DV.CosmicCult;

public abstract class SharedCosmicColossusSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throw = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SleepingSystem _sleeping = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;


    private HashSet<Entity<MobStateComponent>> _mobs = [];
    private HashSet<Entity<PhysicsComponent>> _targets = [];

    private EntityQuery<CosmicCultComponent> _cultistsQuery;
    private EntityQuery<CosmicColossusComponent> _colossiQuery;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CosmicColossusComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<CosmicColossusComponent, EventCosmicColossusSunder>(OnColossusSunder);
        SubscribeLocalEvent<CosmicColossusComponent, EventCosmicColossusHibernate>(OnColossusHibernate);
        SubscribeLocalEvent<CosmicColossusComponent, CosmicHibernationDoAfter>(OnHibernateDoAfter);
        SubscribeLocalEvent<CosmicColossusComponent, EventCosmicColossusEffigy>(OnColossusEffigy);
        SubscribeLocalEvent<CosmicColossusComponent, EventCosmicColossusIngress>(OnColossusIngress);
        SubscribeLocalEvent<CosmicColossusComponent, EventCosmicColossusIngressDoAfter>(OnColossusIngressDoAfter);

        _cultistsQuery = GetEntityQuery<CosmicCultComponent>();
        _colossiQuery = GetEntityQuery<CosmicColossusComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var colossusQuery = EntityQueryEnumerator<CosmicColossusComponent>();
        while (colossusQuery.MoveNext(out var ent, out var comp))
        {
            if (comp.AttackCharge && _timing.CurTime >= comp.AttackAnimationTimer)
            {
                comp.AttackCharge = false;
                _audio.PlayPredicted(comp.SunderSfx, ent, ent);
                PredictedSpawnAtPosition(comp.SunderVfx, Transform(ent).Coordinates);

                _mobs.Clear();
                _lookup.GetEntitiesInRange<MobStateComponent>(Transform(ent).Coordinates, comp.SunderRange, _mobs);
                _mobs.RemoveWhere(target => _cultistsQuery.HasComp(target) || _colossiQuery.HasComp(target));
                foreach (var mob in _mobs)
                {
                    _stun.KnockdownOrStun(mob, comp.SunderStun);
                }

                _targets.Clear();
                _lookup.GetEntitiesInRange<PhysicsComponent>(Transform(ent).Coordinates, comp.SunderRange, _targets);
                _targets.RemoveWhere(target => _cultistsQuery.HasComp(target) || _colossiQuery.HasComp(target) || _container.TryGetOuterContainer(target, Transform(target), out _));
                var userPosision = _transform.GetWorldPosition(ent);
                foreach (var target in _targets)
                {
                    var targetPosition = _transform.GetWorldPosition(target);
                    var direction = (targetPosition - userPosision).Normalized() * comp.SunderThrowDistance;
                    _throw.TryThrow(target, direction, baseThrowSpeed: 10, ent, 0, 0, false, false);
                }
            }
            if (comp.Attacking && _timing.CurTime >= comp.AttackHoldTimer)
            {
                _appearance.SetData(ent, ColossusVisuals.Status, ColossusStatus.Alive);
                _appearance.SetData(ent, ColossusVisuals.Sunder, ColossusAction.Stopped);
                _transform.Unanchor(ent);
                comp.Attacking = false;
            }
            if (comp.Timed && _timing.CurTime >= comp.DeathTimer)
            {
                if (!_threshold.TryGetThresholdForState(ent, MobState.Dead, out var damage))
                    return;
                DamageSpecifier dspec = new();
                dspec.DamageDict.Add("Heat", damage.Value);
                _damage.TryChangeDamage(ent, dspec, true);
            }
            if (_mobState.IsDead(ent) && _timing.CurTime >= comp.DissolveTimer)
            {
                if (comp.DissolveTimer == default!) // The event doesn\t fire on the cleint for some reason and I can't figure out why, shitcode GO!
                {
                    comp.DissolveTimer = _timing.CurTime + comp.DissolveWait;
                    continue;
                }
                if (comp.Container != default!)
                    _container.EmptyContainer(comp.Container);
                _audio.PlayPredicted(comp.DissolveSfx, Transform(ent).Coordinates, ent);
                PredictedSpawnAtPosition(comp.DissolveVfx, Transform(ent).Coordinates);
                PredictedQueueDel(ent);
            }
        }
    }

    private void OnMobStateChanged(Entity<CosmicColossusComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;
        if (TryComp<PhysicsComponent>(ent, out var physComp))
            _physics.SetBodyStatus(ent, physComp, BodyStatus.OnGround, true);
        ent.Comp.Hibernating = false;
        ent.Comp.Attacking = false;
        _appearance.SetData(ent, ColossusVisuals.Status, ColossusStatus.Dead);
        _appearance.SetData(ent, ColossusVisuals.Hibernation, ColossusAction.Stopped);
        _appearance.SetData(ent, ColossusVisuals.Sunder, ColossusAction.Stopped);
        _ambientSound.SetAmbience(ent, false);
        _audio.PlayPredicted(ent.Comp.DeathSfx, ent, ent);
        _popup.PopupPredictedCoordinates(
            Loc.GetString("ghost-role-colossus-death"),
            Transform(ent).Coordinates,
            ent,
            PopupType.Large);
        ent.Comp.DissolveTimer = _timing.CurTime + ent.Comp.DissolveWait;
        if (_mind.TryGetMind(ent, out var mindEnt, out _))
            _mind.TransferTo(mindEnt, Exists(ent.Comp.ImprisonedEntity) ? ent.Comp.ImprisonedEntity : PredictedSpawnAtPosition(ent.Comp.Mindsink, Transform(ent).Coordinates));
    }

    private void OnColossusSunder(Entity<CosmicColossusComponent> ent, ref EventCosmicColossusSunder args)
    {
        args.Handled = true;

        var comp = ent.Comp;
        _appearance.SetData(ent, ColossusVisuals.Status, ColossusStatus.Action);
        _transform.SetCoordinates(ent, args.Target);
        _transform.AnchorEntity(ent);

        comp.Attacking = true;
        comp.AttackCharge = true;
        comp.AttackHoldTimer = comp.AttackWait + _timing.CurTime;
        comp.AttackAnimationTimer = comp.AttackAnimation + _timing.CurTime;
        PredictedSpawnAtPosition(comp.Attack1Vfx, args.Target);
    }

    private void OnColossusHibernate(Entity<CosmicColossusComponent> ent, ref EventCosmicColossusHibernate args)
    {
        if (ent.Comp.Attacking || ent.Comp.Hibernating || !_transform.AnchorEntity(ent))
            return;
        args.Handled = true;
        var comp = ent.Comp;

        comp.Hibernating = true;
        _appearance.SetData(ent, ColossusVisuals.Status, ColossusStatus.Action);
        _appearance.SetData(ent, ColossusVisuals.Hibernation, ColossusAction.Running);
        _statusEffects.TryAddStatusEffectDuration(ent, SleepingSystem.StatusEffectForcedSleeping, comp.HibernationWait);
        _popup.PopupPredictedCoordinates(
            Loc.GetString("ghost-role-colossus-hibernate"),
            Transform(ent).Coordinates,
            ent,
            PopupType.LargeCaution);

        var doAfterArgs = new DoAfterArgs(EntityManager, ent, comp.HibernationWait, new CosmicHibernationDoAfter(), ent)
        {
            NeedHand = false,
            BreakOnWeightlessMove = false,
            BreakOnMove = false,
            BreakOnHandChange = false,
            BreakOnDropItem = false,
            BreakOnDamage = false,
            RequireCanInteract = false,
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnHibernateDoAfter(Entity<CosmicColossusComponent> ent, ref CosmicHibernationDoAfter args)
    {
        _appearance.SetData(ent, ColossusVisuals.Status, ColossusStatus.Alive);
        _appearance.SetData(ent, ColossusVisuals.Hibernation, ColossusAction.Stopped);
        _transform.Unanchor(ent);
        _audio.PlayPredicted(ent.Comp.ReawakenSfx, ent, ent);
        ent.Comp.Hibernating = false;
        PredictedSpawnAtPosition(ent.Comp.CultBigVfx, Transform(ent).Coordinates);
        _sleeping.TryWaking(ent.Owner, force: true);
        if (TryComp<DamageableComponent>(ent, out var damageable))
            _damage.TryChangeDamage(ent.Owner, damageable.Damage / 2 * -1, true);
    }

    protected virtual void OnColossusEffigy(Entity<CosmicColossusComponent> ent, ref EventCosmicColossusEffigy args)
    {
        if (!VerifyPlacement(ent, out var pos) || !TryComp<ActionsComponent>(ent, out var actions))
            return;

        _actions.RemoveAction((ent.Owner, actions), ent.Comp.EffigyPlaceActionEntity);
        PredictedSpawnAtPosition(ent.Comp.EffigyPrototype, pos);
        ent.Comp.Timed = false;
    }

    public bool VerifyPlacement(Entity<CosmicColossusComponent> ent, out EntityCoordinates outPos)
    {
        // MAKE SURE WE'RE STANDING ON A GRID
        var xform = Transform(ent);
        outPos = new EntityCoordinates();

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
        {
            _popup.PopupClient(Loc.GetString("ghost-role-colossus-effigy-error-grid"), ent, ent);
            return false;
        }

        var localTile = _map.GetTileRef(xform.GridUid.Value, grid, xform.Coordinates);
        var targetIndices = localTile.GridIndices + new Vector2i(0, 1);
        var pos = _map.ToCenterCoordinates(xform.GridUid.Value, targetIndices, grid);
        outPos = pos;
        var box = new Box2(pos.Position + new Vector2(-0.4f, -0.4f), pos.Position + new Vector2(0.4f, 0.4f));

        // CHECK IF IT'S BEING PLACED CHEESILY CLOSE TO SPACE
        var spaceDistance = 2;
        var worldPos = _transform.GetWorldPosition(xform);
        foreach (var tile in _map.GetTilesIntersecting(xform.GridUid.Value, grid, new Circle(worldPos, spaceDistance)))
        {
            if (_turf.IsSpace(tile))
            {
                _popup.PopupClient(Loc.GetString("ghost-role-colossus-effigy-error-space", ("DISTANCE", spaceDistance)), ent, ent);
                return false;
            }
        }

        // CHECK FOR ENTITY AND ENVIRONMENTAL INTERSECTIONS
        if (_lookup.AnyLocalEntitiesIntersecting(xform.GridUid.Value, box, LookupFlags.Dynamic | LookupFlags.Static, ent))
        {
            _popup.PopupClient(Loc.GetString("ghost-role-colossus-effigy-error-intersection"), ent, ent);
            return false;
        }

        // IF THE OBJECTIVE OR LOCATION IS MISSING, PLACE IT ANYWHERE
        if (!_mind.TryGetObjectiveComp<CosmicEffigyConditionComponent>(ent, out var obj) || obj.EffigyTarget == null)
            return true;

        var targetXform = Transform(obj.EffigyTarget.Value);
        if (xform.MapID != targetXform.MapID || (_transform.GetWorldPosition(xform) - _transform.GetWorldPosition(targetXform)).LengthSquared() > 15 * 15)
        {
            if (TryComp<WarpPointComponent>(obj.EffigyTarget, out var warp) && warp.Location is not null)
                _popup.PopupClient(Loc.GetString("ghost-role-colossus-effigy-error-location", ("LOCATION", warp.Location)), ent, ent);
            return false;
        }

        return true;
    }

    private void OnColossusIngress(Entity<CosmicColossusComponent> ent, ref EventCosmicColossusIngress args)
    {
        if (TryComp<DoorBoltComponent>(args.Target, out var doorBolt) && doorBolt.BoltsDown)
        {
            _popup.PopupClient(Loc.GetString("cosmicability-ingress-bolted"), ent, ent);
            return;
        }
        var doargs = new DoAfterArgs(EntityManager, ent, ent.Comp.IngressDoAfter, new EventCosmicColossusIngressDoAfter(), ent, args.Target)
        {
            DistanceThreshold = 2f,
            Hidden = false,
            BreakOnMove = true,
        };
        args.Handled = true;
        _audio.PlayPredicted(ent.Comp.DoAfterSfx, ent, ent);
        _doAfter.TryStartDoAfter(doargs);
    }

    private void OnColossusIngressDoAfter(Entity<CosmicColossusComponent> ent, ref EventCosmicColossusIngressDoAfter args)
    {
        if (args.Args.Target is not { } target)
            return;
        if (args.Cancelled || args.Handled)
            return;
        if (TryComp<DoorBoltComponent>(target, out var doorBolt) && doorBolt.BoltsDown)
        {
            _popup.PopupClient(Loc.GetString("cosmicability-ingress-bolted"), ent, ent);
            return;
        }
        args.Handled = true;
        var comp = ent.Comp;

        _door.StartOpening(target);
        _audio.PlayPredicted(comp.IngressSfx, ent, ent);
        PredictedSpawnAtPosition(comp.CultVfx, Transform(target).Coordinates);
    }
}
