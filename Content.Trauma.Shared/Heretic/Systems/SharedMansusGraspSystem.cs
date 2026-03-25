// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Medical.Common.Targeting;
using Content.Shared._EinsteinEngines.Silicon.Components;
using Content.Shared._White.BackStab;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Actions.Events;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Timing;
using Content.Shared.Trigger;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.Ghoul;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Blade;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Rust;
using Content.Trauma.Shared.Heretic.Components.Side;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Heretic.Systems.Abilities;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Cosmos;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Void;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Heretic.Systems;

public abstract class SharedMansusGraspSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ITileDefinitionManager _tile = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    [Dependency] protected readonly Content.Shared.StatusEffectNew.StatusEffectsSystem Status = default!;
    [Dependency] protected readonly TouchSpellSystem TouchSpell = default!;

    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BackStabSystem _backstab = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedVoidCurseSystem _voidCurse = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedStarMarkSystem _starMark = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityLookupSystem _look = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedRatvarianLanguageSystem _language = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedHereticAbilitySystem _ability = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedMechSystem _mech = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public static readonly ProtoId<DamageGroupPrototype> Brute = "Brute";
    public static readonly ProtoId<DamageTypePrototype> Slash = "Slash";

    public static readonly ProtoId<TagPrototype> Bot = "Bot";
    public static readonly ProtoId<TagPrototype> Catwalk = "Catwalk";
    public static readonly ProtoId<TagPrototype> HereticBladeBlade = "HereticBladeBlade";
    public static readonly ProtoId<TagPrototype> Wall = "Wall";

    private readonly HashSet<Entity<MobStateComponent>> _lookupMobs = new();

    public static readonly EntProtoId GraspAffectedStatus = "MansusGraspAffectedStatusEffect";
    public static readonly EntProtoId MansusGrasp = "TouchSpellMansus";

    private static readonly ProtoId<TagPrototype> BladeBladeRitualTag = "RitualBladeBlade";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticCombatMarkOnMeleeHitComponent, MeleeHitEvent>(OnMelee);
        SubscribeLocalEvent<HereticCombatMarkOnMeleeHitComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<AreaMansusGraspComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<AreaMansusGraspComponent, AreaGraspChannelDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<RustGraspComponent, AfterInteractEvent>(OnRustInteract);

        SubscribeLocalEvent<MansusGraspBlockTriggerComponent, AttemptTriggerEvent>(OnAttemptTrigger);
        SubscribeLocalEvent<MansusGraspBlockTriggerComponent, ActionAttemptEvent>(OnActionAttempt);

        SubscribeLocalEvent<MansusGraspComponent, TouchSpellUsedEvent>(OnTouchSpellUsed);

        SubscribeLocalEvent<MindContainerComponent, MansusGraspSpecialEvent>(OnSpecial);
    }

    private void OnSpecial(Entity<MindContainerComponent> ent, ref MansusGraspSpecialEvent args)
    {
        if (InfuseOurBlades(ent))
            args.Invoke = true;
    }

    private bool InfuseOurBlades(EntityUid uid)
    {
        if (!_heretic.TryGetHereticComponent(uid, out var heretic, out _) ||
            heretic.CurrentPath != HereticPath.Blade || heretic.PathStage < 7)
            return false;

        if (!_heretic.TryGetRitual((uid, heretic), BladeBladeRitualTag, out var ritual))
            return false;

        var xformQuery = GetEntityQuery<TransformComponent>();
        var containerEnt = uid;
        if (_container.TryGetOuterContainer(uid, xformQuery.Comp(uid), out var container, xformQuery))
            containerEnt = container.Owner;

        var success = false;
        foreach (var blade in ritual.Value.Comp.LimitedOutput)
        {
            if (!Exists(blade))
                continue;

            if (!_tag.HasTag(blade, HereticBladeBlade))
                continue;

            if (TryComp(blade, out MansusInfusedComponent? infused) &&
                infused.AvailableCharges >= infused.MaxCharges)
                continue;

            if (!_container.TryGetOuterContainer(blade, xformQuery.Comp(blade), out var bladeContainer, xformQuery))
                continue;

            if (bladeContainer.Owner != containerEnt)
                continue;

            var newInfused = EnsureComp<MansusInfusedComponent>(blade);
            newInfused.AvailableCharges = newInfused.MaxCharges;
            success = true;
        }

        return success;
    }

    private void OnTouchSpellUsed(Entity<MansusGraspComponent> ent, ref TouchSpellUsedEvent args)
    {
        var user = args.User;
        var target = args.Target;

        if (!_heretic.TryGetHereticComponent(user, out var heretic, out var mind))
            return;

        if (!TryApplyGraspEffectAndMark(user, (mind, heretic), target, ent, out var triggerGrasp))
            return;

        args.Invoke = true;

        if (!triggerGrasp || !TryComp(target, out StatusEffectsComponent? status))
            return;

        _stun.KnockdownOrStun(target, ent.Comp.KnockdownTime);
        _stamina.TakeStaminaDamage(target, ent.Comp.StaminaDamage);
        _language.DoRatvarian(target, ent.Comp.SpeechTime, true, status);
        Status.TryUpdateStatusEffectDuration(target, GraspAffectedStatus, out _, ent.Comp.AffectedTime);
    }

    private void OnActionAttempt(Entity<MansusGraspBlockTriggerComponent> ent, ref ActionAttemptEvent args)
    {
        if (!Status.HasStatusEffect(args.User, GraspAffectedStatus))
            return;

        _popup.PopupClient(Loc.GetString("mansus-grasp-trigger-fail"), args.User, args.User);
    }

    private void OnAttemptTrigger(Entity<MansusGraspBlockTriggerComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.User is {} user && Status.HasStatusEffect(user, GraspAffectedStatus))
        {
            args.Cancelled = true;
            _popup.PopupClient(Loc.GetString("mansus-grasp-trigger-fail"), user, user);
        }
        else if (Status.HasStatusEffect(Transform(ent).ParentUid, GraspAffectedStatus))
        {
            args.Cancelled = true;
        }
    }

    public float GetAreaGraspRange(Entity<AreaMansusGraspComponent> ent, float time)
    {
        var blend = MathF.Pow(time / (float) ent.Comp.ChannelTime.TotalSeconds, ent.Comp.Slope);
        return MathHelper.Lerp(ent.Comp.MinRange, ent.Comp.MaxRange, blend);
    }

    public TimeSpan CalculateAreaGraspCooldown(float baseCooldown, int hitCount, float range, float multiplier = 2f)
    {
        var cd = baseCooldown * (1f + multiplier * MathF.Pow(range, 0.8f) * (1f - 1f / (MathF.Pow(hitCount, 0.8f) + 1f)));
        return TimeSpan.FromSeconds(cd);
    }

    private void OnDoAfter(Entity<AreaMansusGraspComponent> ent, ref AreaGraspChannelDoAfterEvent args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (!TryComp(ent, out TouchSpellComponent? touchSpell) || args.Handled)
        {
            PredictedQueueDel(ent.Owner);
            return;
        }

        args.Handled = true;

        var time = args.DoAfter.CancelledTime is { } cancelTime
            ? cancelTime - args.DoAfter.StartTime
            : ent.Comp.ChannelTime;
        var range = GetAreaGraspRange(ent, (float) time.TotalSeconds);

        var pos = Transform(args.User).Coordinates;
        _lookupMobs.Clear();
        _look.GetEntitiesInRange(pos, range, _lookupMobs, LookupFlags.Dynamic);

        var targets = new List<EntityUid>();

        foreach (var uid in _lookupMobs)
        {
            if (uid.Owner == args.User)
                continue;

            if (_examine.InRangeUnOccluded(args.User, uid, range))
                targets.Add(uid.Owner);
        }

        var cooldown = CalculateAreaGraspCooldown((float) touchSpell.Cooldown.TotalSeconds, targets.Count, range);
        TouchSpell.UseTouchSpellMultiTarget((ent, touchSpell), args.User, targets, cooldown);
        var spawned = PredictedSpawnAtPosition(ent.Comp.VisualEffect, pos);
        var effect = EnsureComp<AreaGraspEffectComponent>(spawned);
        effect.Size = range;
        effect.SpawnTime = _timing.CurTime;
        Dirty(spawned, effect);
        PredictedQueueDel(ent.Owner);
    }

    private void OnUseInHand(Entity<AreaMansusGraspComponent> ent, ref UseInHandEvent args)
    {
        args.Handled = true;

        var doArgs = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.ChannelTime,
            new AreaGraspChannelDoAfterEvent(),
            ent)
        {
            ColorOverride = ent.Comp.EffectColor,
            BreakOnHandChange = false,
            MultiplyDelay = false,
        };

        if (!_doAfter.TryStartDoAfter(doArgs))
            return;

        _audio.PlayPredicted(ent.Comp.ChannelSound, args.User, args.User);

        ent.Comp.ChannelStartTime = _timing.CurTime;
        Dirty(ent);
    }

    private void OnMapInit(Entity<HereticCombatMarkOnMeleeHitComponent> ent, ref MapInitEvent args)
    {
        ResetPath(ent);
    }

    private void OnMelee(Entity<HereticCombatMarkOnMeleeHitComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || !_heretic.TryGetHereticComponent(args.User, out _, out _) &&
            !HasComp<GhoulComponent>(args.User))
            return;

        foreach (var uid in args.HitEntities)
        {
            if (uid == args.User)
                continue;

            ApplyMark(uid, ent.Comp.NextPath);
        }

        ResetPath(ent);
    }

    private void ResetPath(Entity<HereticCombatMarkOnMeleeHitComponent> ent)
    {
        if (_net.IsClient)
            return;

        ent.Comp.NextPath = _random.Pick(Enum.GetValuesAsUnderlyingType<HereticPath>().Cast<HereticPath>().ToList());
        Dirty(ent);
    }

    public bool TryApplyGraspEffectAndMark(EntityUid user,
        Entity<HereticComponent> heretic,
        EntityUid target,
        EntityUid? grasp,
        out bool triggerGrasp)
    {
        triggerGrasp = true;

        if (heretic.Comp.CurrentPath is not { } path)
            return true;

        if (heretic.Comp.PathStage >= 2)
        {
            if (!ApplyGraspEffect(user, heretic, target, grasp, out var applyMark, out triggerGrasp))
                return false;

            if (!applyMark)
                return true;
        }

        if (heretic.Comp.PathStage >= 3)
            ApplyMark(target, path);

        return true;
    }

    public void ApplyMark(EntityUid target, HereticPath path)
    {
        if (!HasComp<MobStateComponent>(target))
            return;

        RemComp<Trauma.Shared.Heretic.Components.PathSpecific.Cosmos.HereticCosmicMarkComponent>(target);
        var markComp = EnsureComp<HereticCombatMarkComponent>(target);
        markComp.DisappearTime = markComp.MaxDisappearTime;
        markComp.Path = path;
        markComp.Repetitions = path == HereticPath.Ash ? 5 : 1;
        Dirty(target, markComp);
        var ev = new UpdateCombatMarkAppearanceEvent();
        RaiseLocalEvent(target, ref ev);

        if (_net.IsClient || path != HereticPath.Cosmos)
            return;

        var cosmosMark = EnsureComp<Trauma.Shared.Heretic.Components.PathSpecific.Cosmos.HereticCosmicMarkComponent>(target);
        cosmosMark.CosmicDiamondUid = Spawn(cosmosMark.CosmicDiamond, Transform(target).Coordinates);
        _transform.AttachToGridOrMap(cosmosMark.CosmicDiamondUid.Value);
    }

    public bool ApplyGraspEffect(EntityUid performer,
        Entity<HereticComponent> heretic,
        EntityUid target,
        EntityUid? grasp,
        out bool applyMark,
        out bool triggerGrasp)
    {
        applyMark = true;
        triggerGrasp = true;

        switch (heretic.Comp.CurrentPath)
        {
            case HereticPath.Ash:
            {
                var timeSpan = TimeSpan.FromSeconds(5f);
                _statusEffect.TryAddStatusEffect(target,
                    TemporaryBlindnessSystem.BlindingStatusEffect,
                    timeSpan,
                    false,
                    TemporaryBlindnessSystem.BlindingStatusEffect);
                break;
            }

            case HereticPath.Blade:
            {
                if (grasp != null && heretic.Comp.PathStage >= 7 && _tag.HasTag(target, HereticBladeBlade))
                {
                    // empowering blades and shit
                    var infusion = EnsureComp<MansusInfusedComponent>(target);
                    infusion.AvailableCharges = infusion.MaxCharges;
                    break;
                }

                // small stun if the person is looking away or laying down
                if (_backstab.TryBackstab(target, performer, Angle.FromDegrees(45d)))
                {
                    _stun.TryUpdateParalyzeDuration(target, TimeSpan.FromSeconds(1.5f));
                    _damage.TryChangeDamage(target,
                        new DamageSpecifier(_proto.Index(Slash), 10),
                        ignoreResistances: true,
                        origin: performer,
                        targetPart: TargetBodyPart.Chest);
                }

                break;
            }

            case HereticPath.Lock:
            {
                if (TryComp<MechComponent>(target, out var mech) && mech.PilotSlot.ContainedEntity is { } pilot)
                {
                    _mech.TryEject(target, mech, pilot);
                    _stun.TryUpdateParalyzeDuration(pilot, TimeSpan.FromSeconds(5));
                }
                else if (TryComp<DoorComponent>(target, out var door))
                {
                    if (TryComp<DoorBoltComponent>(target, out var doorBolt))
                        _door.SetBoltsDown((target, doorBolt), false);

                    _door.StartOpening(target, door);
                }
                else if (TryComp<AccessReaderComponent>(target, out var access) && !HasComp<MobStateComponent>(target))
                    _access.TryClearAccesses((target, access));
                else
                    break;

                _audio.PlayPredicted(new SoundPathSpecifier("/Audio/_Goobstation/Heretic/hereticknock.ogg"), target, performer);
                _popup.PopupClient(Loc.GetString("heretic-lock-unlocked"), target, performer);

                if (heretic.Comp.PathStage >= 7)
                    return false; // Don't use up grasp when unlocking things at high stage

                break;
            }

            case HereticPath.Flesh:
            {
                if (TryComp<MobStateComponent>(target, out var mobState) && mobState.CurrentState != MobState.Alive &&
                    !HasComp<BorgChassisComponent>(target))
                {
                    if (HasComp<GhoulComponent>(target))
                    {
                        _popup.PopupClient(Loc.GetString("heretic-ability-fail-target-ghoul"), target, performer);
                        break;
                    }

                    if (_net.IsClient) // Client can't see other minds
                    {
                        applyMark = false;
                        triggerGrasp = false;
                        break;
                    }

                    if (!_mind.TryGetMind(target, out _, out _))
                    {
                        _popup.PopupEntity(Loc.GetString("heretic-ability-fail-target-no-mind"), target, performer);
                        break;
                    }

                    var minion = Factory.GetComponent<HereticMinionComponent>();
                    minion.BoundHeretic = performer;
                    minion.MinionId = GetNetEntity(heretic.Owner).Id;
                    AddComp(target, minion, true);

                    EnsureComp<HereticMinionComponent>(target).BoundHeretic = performer;

                    var ghoul = Factory.GetComponent<GhoulComponent>();
                    ghoul.GiveBlade = true;
                    ghoul.DeathBehavior = GhoulDeathBehavior.NoGib;
                    ghoul.CanDeconvert = true;

                    AddComp(target, ghoul);
                    applyMark = false;
                    triggerGrasp = false;
                }

                break;
            }

            case HereticPath.Void:
            {
                _voidCurse.DoCurse(target, 2);
                break;
            }

            case HereticPath.Rust:
            {
                if (TryComp(target, out StationAiHolderComponent? aiHolder)) // Kill AI
                    PredictedQueueDel(aiHolder.Slot.ContainerSlot?.ContainedEntity);
                else if (HasComp<RustGraspComponent>(grasp) && _tag.HasAnyTag(target, Wall, Catwalk) ||
                         HasComp<Rituals.HereticRitualRuneComponent>(
                             target)) // If we have rust grasp and targeting a wall (or a catwalk) - do nothing, let other methods handle that. Also don't damage transmutation rune.
                    return false;
                else if (TryComp(target, out DamageableComponent? damageable) && // Is it even damageable?
                         !HasComp<EdibleComponent>(target) && // Is it not organic body part or organ? (edible)
                         !HasComp<ShadowCloakEntityComponent>(target) && // No instakilling shadow cloak heretics
                         (!HasComp<MobStateComponent>(target) || HasComp<SiliconComponent>(target) ||
                          HasComp<BorgChassisComponent>(target) ||
                          _tag.HasTag(target, Bot))) // Check for ingorganic target
                {
                    _damage.ChangeDamage((target, damageable),
                        new DamageSpecifier(_proto.Index(Brute), 500),
                        ignoreResistances: true,
                        origin: performer,
                        targetPart: TargetBodyPart.Chest);
                }

                break;
            }

            case HereticPath.Cosmos:
            {
                if (_starMark.TryApplyStarMark(target))
                    _starMark.SpawnCosmicField(Transform(performer).Coordinates, heretic.Comp.PathStage, predicted: false);
                break;
            }
        }

        return true;
    }

    private void OnRustInteract(EntityUid uid, RustGraspComponent comp, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!args.CanReach || !_heretic.TryGetHereticComponent(args.User, out var heretic, out _) ||
            !TryComp(uid, out UseDelayComponent? delay) || _delay.IsDelayed((uid, delay), comp.Delay) ||
            !TryComp(uid, out MansusGraspComponent? grasp))
            return;

        if (args.Target is not { } target || _whitelist.IsWhitelistPass(grasp.Blacklist, target))
        {
            RustTile();
            return;
        }

        // Death to catwalks
        if (_tag.HasTag(target, Catwalk))
        {
            args.Handled = true;
            InvokeGrasp(args.User, (uid, grasp));
            ResetDelay(comp.CatwalkDelayMultiplier);
            PredictedDel(target);
            return;
        }

        if (!_ability.TryMakeRustWall(target, args.User, heretic))
            return;

        args.Handled = true;
        InvokeGrasp(args.User, (uid, grasp));
        ResetDelay();

        return;

        void RustTile()
        {
            if (!args.ClickLocation.IsValid(EntityManager))
                return;

            if (!_mapManager.TryFindGridAt(_transform.ToMapCoordinates(args.ClickLocation), out var gridUid, out var mapGrid))
                return;

            var tileRef = _mapSystem.GetTileRef(gridUid, mapGrid, args.ClickLocation);
            var tileDef = (ContentTileDefinition) _tile[tileRef.Tile.TypeId];

            if (!_ability.CanRustTile(tileDef))
                return;

            args.Handled = true;
            ResetDelay();
            InvokeGrasp(args.User, (uid, grasp));

            _ability.MakeRustTile(gridUid, mapGrid, tileRef, comp.TileRune);
        }

        void ResetDelay(float multiplier = 1f)
        {
            // Less delay the higher the path stage is
            var length = float.Lerp(comp.MaxUseDelay, comp.MinUseDelay, heretic.PathStage / 10f) * multiplier;
            _delay.SetLength((uid, delay), TimeSpan.FromSeconds(length), comp.Delay);
            _delay.TryResetDelay((uid, delay), false, comp.Delay);
        }
    }
}

[Serializable, NetSerializable]
public sealed partial class AreaGraspChannelDoAfterEvent : SimpleDoAfterEvent;
