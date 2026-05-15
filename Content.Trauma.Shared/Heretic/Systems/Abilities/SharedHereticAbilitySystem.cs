// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Religion;
using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Medical.Shared.Traumas;
using Content.Medical.Shared.Wounds;
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
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Heretic.Systems.Abilities;

public abstract partial class SharedHereticAbilitySystem : EntitySystem
{
    [Dependency] private IMapManager _mapMan = default!;
    [Dependency] private INetManager _net = default!;

    [Dependency] protected IPrototypeManager Proto = default!;
    [Dependency] protected ITileDefinitionManager Tile = default!;
    [Dependency] protected IRobustRandom Random = default!;
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected SharedDoAfterSystem DoAfter = default!;
    [Dependency] protected EntityLookupSystem Lookup = default!;
    [Dependency] protected StatusEffectsSystem Status = default!;
    [Dependency] protected SharedVoidCurseSystem Voidcurse = default!;
    [Dependency] protected SharedHereticSystem Heretic = default!;
    [Dependency] protected Content.Shared.StatusEffectNew.StatusEffectsSystem StatusNew = default!;
    [Dependency] protected ExamineSystemShared Examine = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;

    [Dependency] private SharedProjectileSystem _projectile = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ThrowingSystem _throw = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedStarMarkSystem _starMark = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedEyeSystem _eye = default!;
    [Dependency] private DamageableSystem _dmg = default!;
    [Dependency] private MobThresholdSystem _mobThreshold = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedBloodstreamSystem _blood = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private SharedEmpSystem _emp = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private SharedCuffableSystem _cuffs = default!;
    [Dependency] private SharedEnsnareableSystem _snare = default!;
    [Dependency] private SharedMansusGraspSystem _grasp = default!;
    [Dependency] private TouchSpellSystem _touchSpell = default!;
    [Dependency] private TraumaSystem _trauma = default!;
    [Dependency] private SharedGhoulSystem _ghoul = default!;

    [Dependency] private EntityQuery<GhoulComponent> _ghoulQuery = default!;

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
        {
            args.Cancelled = true;
            return;
        }

        if (!Proto.Index(args.Args.TouchSpell).HasComponent<MansusGraspComponent>())
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
        return Proto.HasIndex(pathSpecific) ? pathSpecific : ent.Comp.MansusGraspProto;
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
    /// <param name="boneHeal">how much to heal bone damage, null = full heal</param>
    public void IHateWoundMed(Entity<DamageableComponent?, BodyComponent?> uid,
        DamageSpecifier? toHeal,
        FixedPoint2? bloodHeal,
        FixedPoint2? bleedHeal,
        FixedPoint2? boneHeal)
    {
        if (!Resolve(uid, ref uid.Comp1, false))
            return;

        if (toHeal != null)
        {
            _dmg.ChangeDamage((uid, uid.Comp1),
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

        if (boneHeal == null || boneHeal != FixedPoint2.Zero && Resolve(uid, ref uid.Comp2, false))
        {
            var parts = _body.GetOrgans<WoundableComponent>((uid, uid.Comp2));

            foreach (var part in parts)
            {
                if (_trauma.GetBone(part.AsNullable()) is not {} bone)
                    continue;

                if (boneHeal is { } heal)
                    _trauma.ApplyDamageToBone(bone, heal, bone.Comp);
                else
                    _trauma.SetBoneIntegrity(bone, bone.Comp.BoneIntegrity, bone.Comp);
            }
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
