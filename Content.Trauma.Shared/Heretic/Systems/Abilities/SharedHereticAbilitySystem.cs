// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Religion;
using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Cuffs;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Emp;
using Content.Shared.Ensnaring;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Jaunt;
using Content.Shared.Magic.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Prototypes;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Trauma.Common.CollectiveMind;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.Ghoul;
using Content.Trauma.Shared.Heretic.Components.StatusEffects;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Cosmos;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Void;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Heretic.Systems.Abilities;

public abstract partial class SharedHereticAbilitySystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly INetManager _net = default!;

    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedDoAfterSystem DoAfter = default!;
    [Dependency] protected readonly EntityLookupSystem Lookup = default!;
    [Dependency] protected readonly StatusEffectsSystem Status = default!;
    [Dependency] protected readonly SharedVoidCurseSystem Voidcurse = default!;
    [Dependency] protected readonly SharedHereticSystem Heretic = default!;
    [Dependency] protected readonly Content.Shared.StatusEffectNew.StatusEffectsSystem StatusNew = default!;
    [Dependency] protected readonly ExamineSystemShared Examine = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    [Dependency] private readonly SharedProjectileSystem _projectile = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throw = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedStarMarkSystem _starMark = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly DamageableSystem _dmg = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly SharedBloodstreamSystem _blood = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedEmpSystem _emp = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedCuffableSystem _cuffs = default!;
    [Dependency] private readonly SharedEnsnareableSystem _snare = default!;
    [Dependency] private readonly SharedMansusGraspSystem _grasp = default!;
    [Dependency] private readonly TouchSpellSystem _touchSpell = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly EntityQuery<GhoulComponent> _ghoulQuery = default!;

    public static readonly DamageSpecifier AllDamage = new()
    {
        DamageDict =
        {
            { "Blunt", 1 },
            { "Slash", 1 },
            { "Piercing", 1 },
            { "Heat", 1 },
            { "Cold", 1 },
            { "Shock", 1 },
            { "Asphyxiation", 1 },
            { "Bloodloss", 1 },
            { "Caustic", 1 },
            { "Poison", 1 },
            { "Radiation", 1 },
            { "Cellular", 1 },
            { "Ion", 1 },
            { "Holy", 1 },
        },
    };

    public static ProtoId<CollectiveMindPrototype> MansusLinkMind = "MansusLink";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAsh();
        SubscribeBlade();
        SubscribeRust();
        SubscribeCosmos();
        SubscribeVoid();
        SubscribeFlesh();
        SubscribeSide();
        SubscribeLock();

        SubscribeLocalEvent<HereticActionComponent, BeforeCastSpellEvent>(OnBeforeCast);
        SubscribeLocalEvent<HereticActionComponent, ActionAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<JauntComponent, HereticMagicCastAttemptEvent>(OnJauntMagicAttempt);

        SubscribeLocalEvent<MindContainerComponent, BeforeTouchSpellAbilityUsedEvent>(OnBeforeTouchSpell);
    }

    private void OnBeforeTouchSpell(Entity<MindContainerComponent> ent, ref BeforeTouchSpellAbilityUsedEvent args)
    {
        if (!TryUseAbility(args.Args, false))
            return;

        if (!_proto.Index(args.Args.TouchSpell).HasComponent<MansusGraspComponent>())
            return;

        if (!Heretic.TryGetHereticComponent(ent.AsNullable(), out var heretic, out var mind))
            return;

        args.TouchSpell = GetMansusGraspProto((mind, heretic));
    }

    private string GetMansusGraspProto(Entity<HereticComponent> ent)
    {
        if (ent.Comp.PathStage < 2)
            return ent.Comp.MansusGraspProto;

        var pathSpecific = ent.Comp.MansusGraspProto + ent.Comp.CurrentPath;
        return _proto.HasIndex(pathSpecific) ? pathSpecific : ent.Comp.MansusGraspProto;
    }

    private void OnAttempt(Entity<HereticActionComponent> ent, ref ActionAttemptEvent args)
    {
        if (StatusNew .HasEffectComp<BlockHereticActionsStatusEffectComponent>( args.User))
            args.Cancelled = true;
    }

    protected List<Entity<MobStateComponent>> GetNearbyPeople(EntityUid ent,
        float range,
        HereticPath? path,
        EntityCoordinates? coords = null,
        bool checkNullRod = true)
    {
        var list = new List<Entity<MobStateComponent>>();
        var lookup = Lookup.GetEntitiesInRange<MobStateComponent>(coords ?? Transform(ent).Coordinates, range);

        foreach (var look in lookup)
        {
            // ignore ghouls and heretics with the same path, affect everyone else
            if (_ghoulQuery.HasComp(look) || Heretic.TryGetHereticComponent(look.Owner, out var th, out _) && th.CurrentPath == path)
                continue;

            if (checkNullRod)
            {
                var ev = new BeforeCastTouchSpellEvent(look, false);
                RaiseLocalEvent(look, ev, true);
                if (ev.Cancelled)
                    continue;
            }

            list.Add(look);
        }

        return list;
    }

    public bool TryUseAbility(BaseActionEvent args, bool handle = true)
    {
        if (args.Handled)
            return false;
        var ev = new BeforeCastSpellEvent(args.Performer);
        RaiseLocalEvent(args.Action, ref ev);
        var result = !ev.Cancelled;
        if (result && handle)
            args.Handled = true;
        return result;
    }

    private void OnJauntMagicAttempt(Entity<JauntComponent> ent, ref HereticMagicCastAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnBeforeCast(Entity<HereticActionComponent> ent, ref BeforeCastSpellEvent args)
    {
        var attemptEv = new HereticMagicCastAttemptEvent(args.Performer, ent);
        RaiseLocalEvent(args.Performer, ref attemptEv);
        if (attemptEv.Cancelled)
        {
            args.Cancelled = true;
            return;
        }

        if (HasComp<GhoulComponent>(args.Performer) ||
            HasComp<Components.PathSpecific.Cosmos.StarGazerComponent>(args.Performer))
            return;

        if (!Heretic.TryGetHereticComponent(args.Performer, out var heretic, out _))
        {
            args.Cancelled = true;
            return;
        }

        if (!ent.Comp.RequireMagicItem || heretic.Ascended)
            return;

        var ev = new CheckMagicItemEvent();
        RaiseLocalEvent(args.Performer, ev);

        if (ev.Handled)
            return;

        // Almost all of the abilites are serverside anyway
        if (_net.IsServer)
            Popup.PopupEntity(Loc.GetString("heretic-ability-fail-magicitem"), args.Performer, args.Performer);

        args.Cancelled = true;
    }

    protected EntityUid ShootProjectileSpell(EntityUid performer,
        EntityCoordinates coords,
        EntProtoId toSpawn,
        float speed,
        EntityUid? target)
    {
        var xform = Transform(performer);
        var fromCoords = xform.Coordinates;
        var toCoords = coords;

        var fromMap = _transform.ToMapCoordinates(fromCoords);
        var spawnCoords = _mapMan.TryFindGridAt(fromMap, out var gridUid, out _)
            ? _transform.WithEntityId(fromCoords, gridUid)
            : new(_map.GetMap(fromMap.MapId), fromMap.Position);

        var userVelocity = _physics.GetMapLinearVelocity(spawnCoords);

        var projectile = PredictedSpawnAtPosition(toSpawn, spawnCoords);
        var direction = _transform.ToMapCoordinates(toCoords).Position -
                        _transform.ToMapCoordinates(spawnCoords).Position;
        _gun.ShootProjectile(projectile, direction, userVelocity, performer, performer, speed);

        if (target != null)
            _gun.SetTarget(projectile, target.Value, out _);

        return projectile;
    }

    /// <summary>
    /// Heals everything imaginable
    /// </summary>
    /// <param name="uid">Entity to heal</param>
    /// <param name="toHeal">how much to heal, null = full heal</param>
    /// <param name="bloodHeal">how much to restore blood, null = fully restore</param>
    /// <param name="bleedHeal">how much to heal bleeding, null = full heal</param>
    public void IHateWoundMed(Entity<DamageableComponent?, BodyComponent?> uid,
        DamageSpecifier? toHeal,
        FixedPoint2? bloodHeal,
        FixedPoint2? bleedHeal)
    {
        if (!Resolve(uid, ref uid.Comp1, false))
            return;

        if (toHeal != null)
        {
            _dmg.TryChangeDamage((uid, uid.Comp1),
                toHeal,
                true,
                false,
                targetPart: TargetBodyPart.All,
                splitDamage: SplitDamageBehavior.SplitEnsureAll);
        }
        else
        {
            TryComp<MobThresholdsComponent>(uid, out var thresholds);
            // do this so that the state changes when we set the damage
            _mobThreshold.SetAllowRevives(uid, true, thresholds);
            _dmg.SetAllDamage((uid, uid.Comp1), 0);
            _mobThreshold.SetAllowRevives(uid, false, thresholds);
        }

        // im too lazy to update some unused shit to reduce pain by an arbitrary number (makes no fucking sense)
        // have this shit instead
        var painEv = new LifeStealHealEvent();
        RaiseLocalEvent(uid, ref painEv);

        if (bleedHeal == FixedPoint2.Zero && bloodHeal == FixedPoint2.Zero ||
            !TryComp(uid, out BloodstreamComponent? blood))
            return;

        if (bleedHeal != FixedPoint2.Zero && blood.BleedAmount > 0f)
        {
            if (bleedHeal == null)
                _blood.TryModifyBleedAmount((uid, blood), -blood.BleedAmount);
            else
                _blood.TryModifyBleedAmount((uid, blood), bleedHeal.Value.Float());
        }

        if (bloodHeal == FixedPoint2.Zero || !TryComp(uid, out SolutionContainerManagerComponent? sol) ||
            !_solution.ResolveSolution((uid, sol), blood.BloodSolutionName, ref blood.BloodSolution) ||
            blood.BloodSolution.Value.Comp.Solution.Volume >= blood.BloodReferenceSolution.Volume)
            return;

        var missing = blood.BloodReferenceSolution.Volume - blood.BloodSolution.Value.Comp.Solution.Volume;
        if (bloodHeal == null)
        {
            _blood.TryModifyBloodLevel((uid, blood), missing);
        }
        else
        {
            _blood.TryModifyBloodLevel((uid, blood), FixedPoint2.Min(bloodHeal.Value, missing));
        }
    }
}
