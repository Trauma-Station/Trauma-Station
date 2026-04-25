// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Actions.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
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
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Rust;
using Content.Trauma.Shared.Heretic.Components.Side;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Heretic.Rituals;
using Content.Trauma.Shared.Heretic.Systems.Abilities;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Heretic.Systems;

public abstract class SharedMansusGraspSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ITileDefinitionManager _tile = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    [Dependency] protected readonly Content.Shared.StatusEffectNew.StatusEffectsSystem Status = default!;
    [Dependency] protected readonly TouchSpellSystem TouchSpell = default!;

    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityLookupSystem _look = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedRatvarianLanguageSystem _language = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedHereticAbilitySystem _ability = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly HereticRitualEffectSystem _effects = default!;
    [Dependency] private readonly SharedHereticRitualSystem _ritual = default!;

    private readonly HashSet<Entity<MobStateComponent>> _lookupMobs = new();

    public static readonly EntProtoId GraspAffectedStatus = "MansusGraspAffectedStatusEffect";

    public static readonly EntityWhitelist GraspWhitelist = new()
    {
        Components = ["MansusGrasp"],
    };

    public static readonly ProtoId<TagPrototype> HereticBladeBlade = "HereticBladeBlade";
    private static readonly ProtoId<TagPrototype> BladeBladeRitualTag = "RitualBladeBlade";

    public const string Grasp = "Grasp";
    public const string InvokeGrasp = "InvokeGrasp";
    public const string ApplyGraspDefaultEffects = "ApplyGraspDefaultEffects";
    public const string ApplyGraspMark = "ApplyGraspMark";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticCombatMarkOnMeleeHitComponent, MeleeHitEvent>(OnMelee);
        SubscribeLocalEvent<HereticCombatMarkOnMeleeHitComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<AreaMansusGraspComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<AreaMansusGraspComponent, AreaGraspChannelDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<RustGraspComponent, AfterInteractEvent>(OnRustInteract,
            before: new[] { typeof(TouchSpellSystem) });
        SubscribeLocalEvent<RustGraspComponent, TouchSpellAttemptEvent>(OnRustAttempt);

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

        TryApplyGraspEffectAndMark(user, (mind, heretic), target, ent, out var invokeGrasp, out var triggerGrasp);

        args.Invoke = true;
        if (!invokeGrasp)
            args.CooldownOverride = TimeSpan.Zero;

        if (!triggerGrasp || !TryComp(target, out StatusEffectsComponent? status))
            return;

        _stun.KnockdownOrStun(target, ent.Comp.KnockdownTime);
        _stamina.TakeStaminaDamage(target, ent.Comp.StaminaDamage, source: args.User, ignoreResist: true);
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
        if (args.User is { } user && Status.HasStatusEffect(user, GraspAffectedStatus))
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
        var cd = baseCooldown *
                 (1f + multiplier * MathF.Pow(range, 0.8f) * (1f - 1f / (MathF.Pow(hitCount, 0.8f) + 1f)));
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

    public void TryApplyGraspEffectAndMark(EntityUid user,
        Entity<HereticComponent> heretic,
        EntityUid target,
        Entity<MansusGraspComponent> grasp,
        out bool invokeGrasp,
        out bool triggerGrasp)
    {
        triggerGrasp = true;
        invokeGrasp = true;

        if (heretic.Comp.CurrentPath is not { } path)
            return;

        if (heretic.Comp.PathStage >= 2)
        {
            ApplyGraspEffect(user, heretic, target, grasp, out invokeGrasp, out var applyMark, out triggerGrasp);

            if (!applyMark)
                return;
        }

        if (heretic.Comp.PathStage >= 3)
            ApplyMark(target, path, heretic.Comp.PathStage);
    }

    public void ApplyMark(EntityUid target, HereticPath path, int pathStage = 3)
    {
        if (!HasComp<MobStateComponent>(target))
            return;

        RemComp<HereticCosmicMarkComponent>(target);
        var markComp = EnsureComp<HereticCombatMarkComponent>(target);
        markComp.DisappearTime = markComp.MaxDisappearTime;
        markComp.Path = path;
        markComp.Repetitions = path == HereticPath.Ash ? 5 : 1;
        Dirty(target, markComp);
        var ev = new UpdateCombatMarkAppearanceEvent();
        RaiseLocalEvent(target, ref ev);

        if (_net.IsClient || path != HereticPath.Cosmos)
            return;

        var cosmosMark = EnsureComp<HereticCosmicMarkComponent>(target);
        cosmosMark.CosmicDiamondUid = Spawn(cosmosMark.CosmicDiamond, Transform(target).Coordinates);
        cosmosMark.PathStage = pathStage;
        _transform.AttachToGridOrMap(cosmosMark.CosmicDiamondUid.Value);
        Dirty(target, cosmosMark);
    }

    public void ApplyGraspEffect(EntityUid performer,
        Entity<HereticComponent> heretic,
        EntityUid target,
        Entity<MansusGraspComponent> grasp,
        out bool invokeGrasp,
        out bool applyMark,
        out bool triggerGrasp)
    {
        var raiser = EnsureComp<HereticRitualRaiserComponent>(grasp);

        Entity<HereticRitualRaiserComponent> ent = (grasp, raiser);

        ResetBlackboard(ent, performer, heretic);

        if (grasp.Comp.Effects is { } effects)
            _effects.ApplyEffects(target, effects, ent, performer);

        _ritual.TryGetValue(ent, InvokeGrasp, out invokeGrasp);
        _ritual.TryGetValue(ent, ApplyGraspMark, out applyMark);
        _ritual.TryGetValue(ent, ApplyGraspDefaultEffects, out triggerGrasp);
        raiser.Blackboard.Clear();
    }

    private void ResetBlackboard(Entity<HereticRitualRaiserComponent> grasp, EntityUid performer, EntityUid mind)
    {
        grasp.Comp.Blackboard.Clear();
        grasp.Comp.Blackboard[Grasp] = grasp.Owner;
        grasp.Comp.Blackboard[InvokeGrasp] = true;
        grasp.Comp.Blackboard[ApplyGraspDefaultEffects] = true;
        grasp.Comp.Blackboard[ApplyGraspMark] = true;
        grasp.Comp.Blackboard[SharedHereticRitualSystem.Performer] = performer;
        grasp.Comp.Blackboard[SharedHereticRitualSystem.Mind] = mind;
    }

    private void OnRustAttempt(Entity<RustGraspComponent> ent, ref TouchSpellAttemptEvent args)
    {
        args.Cancelled = _delay.IsDelayed(ent.Owner, ent.Comp.Delay);
    }

    private void OnRustInteract(EntityUid uid, RustGraspComponent comp, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!args.CanReach || !_heretic.TryGetHereticComponent(args.User, out var heretic, out _) ||
            !TryComp(uid, out UseDelayComponent? delay) || _delay.IsDelayed((uid, delay), comp.Delay) ||
            args.Target != null)
            return;

        if (!args.ClickLocation.IsValid(EntityManager))
            return;

        if (!_mapManager.TryFindGridAt(_transform.ToMapCoordinates(args.ClickLocation),
                out var gridUid,
                out var mapGrid))
            return;

        var tileRef = _mapSystem.GetTileRef(gridUid, mapGrid, args.ClickLocation);
        var tileDef = (ContentTileDefinition) _tile[tileRef.Tile.TypeId];

        if (!_ability.CanRustTile(tileDef))
            return;

        args.Handled = true;
        ResetRustGraspDelay((uid, comp, delay), heretic.PathStage);
        _ability.MakeRustTile(gridUid, mapGrid, tileRef, comp.TileRune);
        TouchSpell.InvokeTouchSpell(uid, args.User, TimeSpan.Zero);
    }

    public void ResetRustGraspDelay(Entity<RustGraspComponent, UseDelayComponent> ent,
        int pathStage,
        float multiplier = 1f)
    {
        var (uid, comp, delay) = ent;
        // Less delay the higher the path stage is
        var length = float.Lerp(comp.MaxUseDelay, comp.MinUseDelay, pathStage / 10f) * multiplier;
        _delay.SetLength((uid, delay), TimeSpan.FromSeconds(length), comp.Delay);
        _delay.TryResetDelay((uid, delay), false, comp.Delay);
    }
}

[Serializable, NetSerializable]
public sealed partial class AreaGraspChannelDoAfterEvent : SimpleDoAfterEvent;
