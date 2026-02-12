// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.CCVar;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Traumas;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.CPUJob.JobQueues.Queues;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Medical.Shared.Wounds;

public sealed partial class WoundSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly BodyPartSystem _part = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;

    // I'm the one.... who throws........
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly TraumaSystem _trauma = default!;

    private EntityQuery<WoundComponent> _query;

    private float _medicalHealingTickrate = 0.5f;
    private TimeSpan _nextUpdate;
    private TimeSpan _minimumTimeBeforeHeal = TimeSpan.FromSeconds(2f);

    private const double WoundJobTime = 0.005;
    private readonly JobQueue _woundJobQueue = new(WoundJobTime);

    public sealed class WoundJob : Job<object>
    {
        private readonly WoundSystem _self;
        private readonly Entity<WoundableComponent> _ent;
        private readonly EntityUid _bodyEnt;
        public WoundJob(WoundSystem self, Entity<WoundableComponent> ent, EntityUid bodyEnt, double maxTime, CancellationToken cancellation = default) : base(maxTime, cancellation)
        {
            _self = self;
            _ent = ent;
            _bodyEnt = bodyEnt;
        }

        public WoundJob(WoundSystem self, Entity<WoundableComponent> ent, EntityUid bodyEnt, double maxTime, IStopwatch stopwatch, CancellationToken cancellation = default) : base(maxTime, stopwatch, cancellation)
        {
            _self = self;
            _ent = ent;
            _bodyEnt = bodyEnt;
        }

        protected override Task<object?> Process()
        {
            _self.ProcessHealing(_ent, _bodyEnt);

            return Task.FromResult<object?>(null);
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<WoundComponent>();

        SubscribeLocalEvent<WoundComponent, ComponentGetState>(OnWoundComponentGet);
        SubscribeLocalEvent<WoundComponent, ComponentHandleState>(OnWoundComponentHandleState);
        SubscribeLocalEvent<WoundableComponent, ComponentGetState>(OnWoundableComponentGet);
        SubscribeLocalEvent<WoundableComponent, ComponentHandleState>(OnWoundableComponentHandleState);
        InitWounding();

        Subs.CVar(_cfg, SurgeryCVars.MedicalHealingTickrate, val => _medicalHealingTickrate = val, true);
        Subs.CVar(_cfg, SurgeryCVars.MinimumTimeBeforeHeal, val => _minimumTimeBeforeHeal = TimeSpan.FromSeconds(val), true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _woundJobQueue.Process();

        if (!_timing.IsFirstTimePredicted)
            return;

        var now = _timing.CurTime;
        if (now < _nextUpdate)
            return;

        _nextUpdate = now + TimeSpan.FromSeconds(1f / _medicalHealingTickrate);

        // If this still causes lag, we go with the nuclear option of also checking for ConsciousnessComponent :niceportrait:
        using var query = EntityQueryEnumerator<BodyComponent, DamageableComponent>();
        while (query.MoveNext(out var ent, out var body, out var damageable))
        {
            if (TerminatingOrDeleted(ent)
                || now - damageable.LastModifiedTime < _minimumTimeBeforeHeal
                || _mobState.IsIncapacitated(ent))
                continue;

            foreach (var woundable in _body.GetOrgans<WoundableComponent>((ent, body)))
            {
                if (woundable.Comp.CanHealDamage || woundable.Comp.CanHealBleeds)
                    _woundJobQueue.EnqueueJob(new WoundJob(this, woundable, ent, WoundJobTime));
            }
        }
    }

    private void ProcessHealing(Entity<WoundableComponent> woundable, EntityUid bodyEnt)
    {
        if (woundable.Comp.CanHealBleeds)
            TryHealBleedingWounds(woundable, (float) -woundable.Comp.BleedingTreatmentAbility, out _, woundable);

        if (!woundable.Comp.CanHealDamage)
            return;

        var woundsToHeal = GetWoundableWounds(woundable)
            .Where(wound => CanHealWound(wound, wound))
            .ToList();

        var healAmount = -woundable.Comp.HealAbility / woundsToHeal.Count;
        var damageSpecifier = new DamageSpecifier();
        var anythingToHeal = false;
        foreach (var wound in woundsToHeal)
        {
            if (wound.Comp.SelfHealMultiplier <= 0)
                continue;

            var damageType = wound.Comp.DamageType;
            var adjustedHealAmount = ApplyHealingRateMultipliers(wound, woundable, healAmount);

            if (adjustedHealAmount != 0)
                anythingToHeal = true;

            if (damageSpecifier.DamageDict.TryGetValue(damageType, out var existingAmount))
                damageSpecifier.DamageDict[damageType] = existingAmount + adjustedHealAmount;
            else
                damageSpecifier.DamageDict.TryAdd(damageType, adjustedHealAmount);
        }

        if (!anythingToHeal || !TryComp<BodyPartComponent>(woundable, out var part))
            return;

        _damageable.TryChangeDamage(bodyEnt,
            damageSpecifier,
            ignoreResistances: false,
            targetPart: _body.GetTargetBodyPart(part.PartType, part.Symmetry));
    }

    private void OnWoundComponentGet(EntityUid uid, WoundComponent comp, ref ComponentGetState args)
    {
        var state = new WoundComponentState
        {
            HoldingWoundable =
                TryGetNetEntity(comp.HoldingWoundable, out var holdingWoundable)
                    ? holdingWoundable.Value
                    : NetEntity.Invalid,

            WoundSeverityPoint = comp.WoundSeverityPoint,

            WoundType = comp.WoundType,

            DamageGroup = comp.DamageGroup,
            DamageType = comp.DamageType,

            ScarWound = comp.ScarWound,
            IsScar = comp.IsScar,

            WoundSeverity = comp.WoundSeverity,

            WoundVisibility = comp.WoundVisibility,

            CanBeHealed = comp.CanBeHealed,
            SelfHealMultiplier = comp.SelfHealMultiplier
        };

        args.State = state;
    }

    private void OnWoundComponentHandleState(EntityUid uid, WoundComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not WoundComponentState state)
            return;

        // Predict events on client!!
        var holdingWoundable = TryGetEntity(state.HoldingWoundable, out var e) ? e.Value : EntityUid.Invalid;
        if (holdingWoundable != component.HoldingWoundable)
        {
            if (holdingWoundable == EntityUid.Invalid)
            {
                if (TryComp(holdingWoundable, out WoundableComponent? oldParentWoundable) &&
                    TryComp(oldParentWoundable.RootWoundable, out WoundableComponent? oldWoundableRoot))
                {
                    var ev2 = new WoundRemovedEvent(component, oldParentWoundable, oldWoundableRoot);
                    RaiseLocalEvent(component.HoldingWoundable, ref ev2);
                }
            }
            else
            {
                if (TryComp(holdingWoundable, out WoundableComponent? parentWoundable) &&
                    TryComp(parentWoundable.RootWoundable, out WoundableComponent? woundableRoot))
                {
                    var ev = new WoundAddedEvent(component, parentWoundable, woundableRoot);
                    RaiseLocalEvent(uid, ref ev);
                    RaiseLocalEvent(holdingWoundable, ref ev);

                    if (_body.GetBody(holdingWoundable) is {} body)
                    {
                        var bodyEv = new WoundAddedOnBodyEvent((uid, component), parentWoundable, woundableRoot);
                        RaiseLocalEvent(body, ref bodyEv);
                    }
                }
            }
        }

        component.HoldingWoundable = holdingWoundable;

        if (component.WoundSeverityPoint != state.WoundSeverityPoint)
        {
            var ev = new WoundSeverityPointChangedEvent(component,
                component.WoundSeverityPoint,
                state.WoundSeverityPoint);
            RaiseLocalEvent(uid, ref ev);

            // TODO: On body changed events aren't predicted, welp
        }

        component.WoundSeverityPoint = state.WoundSeverityPoint;

        if (component.HoldingWoundable != EntityUid.Invalid)
        {
            UpdateWoundableIntegrity(component.HoldingWoundable);
            CheckWoundableSeverityThresholds(component.HoldingWoundable);
        }

        component.WoundType = state.WoundType;

        component.DamageGroup = state.DamageGroup;
        if (state.DamageType != null)
            component.DamageType = state.DamageType;

        component.ScarWound = state.ScarWound;
        component.IsScar = state.IsScar;

        if (component.WoundSeverity != state.WoundSeverity)
        {
            var ev = new WoundSeverityChangedEvent(component.WoundSeverity, state.WoundSeverity);
            RaiseLocalEvent(uid, ref ev);
        }

        component.WoundSeverity = state.WoundSeverity;
        component.WoundVisibility = state.WoundVisibility;
        component.CanBeHealed = state.CanBeHealed;
        component.SelfHealMultiplier = state.SelfHealMultiplier;
    }

    private void OnWoundableComponentGet(EntityUid uid, WoundableComponent comp, ref ComponentGetState args)
    {
        var state = new WoundableComponentState
        {
            ParentWoundable = TryGetNetEntity(comp.ParentWoundable, out var parentWoundable) ? parentWoundable : null,
            RootWoundable = TryGetNetEntity(comp.RootWoundable, out var rootWoundable)
                ? rootWoundable.Value
                : NetEntity.Invalid,

            ChildWoundables =
                comp.ChildWoundables
                    .Select(woundable => TryGetNetEntity(woundable, out var ne)
                        ? ne.Value
                        : NetEntity.Invalid)
                    .ToHashSet(),
            // Attached and Detached -Woundable events are handled on client with containers

            AllowWounds = comp.AllowWounds,
            CanRemove = comp.CanRemove,
            CanBleed = comp.CanBleed,

            DamageContainerID = comp.DamageContainerID,

            DodgeChance = comp.DodgeChance,
            Bleeds = comp.Bleeds,
            WoundableIntegrity = comp.WoundableIntegrity,
            HealAbility = comp.HealAbility,

            SeverityMultipliers =
                comp.SeverityMultipliers
                    .Select(multiplier
                        => (TryGetNetEntity(multiplier.Key, out var ne) ? ne.Value : NetEntity.Invalid,
                            multiplier.Value))
                    .ToDictionary(),
            HealingMultipliers =
                comp.HealingMultipliers
                    .Select(multiplier
                        => (TryGetNetEntity(multiplier.Key, out var ne) ? ne.Value : NetEntity.Invalid,
                            multiplier.Value))
                    .ToDictionary(),

            WoundableSeverity = comp.WoundableSeverity,
        };

        args.State = state;
    }

    private void OnWoundableComponentHandleState(EntityUid uid, WoundableComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not WoundableComponentState state)
            return;

        TryGetEntity(state.ParentWoundable, out component.ParentWoundable);
        TryGetEntity(state.RootWoundable, out var rootWoundable);
        component.RootWoundable = rootWoundable ?? EntityUid.Invalid;

        component.ChildWoundables = state.ChildWoundables
            .Select(x => TryGetEntity(x, out var y) ? y.Value : EntityUid.Invalid)
            .Where(x => x.Valid)
            .ToHashSet();
        // Attached and Detached -Woundable events are handled on client with containers

        component.AllowWounds = state.AllowWounds;
        component.CanRemove = state.CanRemove;
        component.CanBleed = state.CanBleed;

        component.DamageContainerID = state.DamageContainerID;

        component.DodgeChance = state.DodgeChance;
        component.HealAbility = state.HealAbility;
        component.Bleeds = state.Bleeds;

        component.SeverityMultipliers =
            state.SeverityMultipliers
                .Select(multiplier
                    => (TryGetEntity(multiplier.Key, out var ne) ? ne.Value : EntityUid.Invalid, multiplier.Value))
                .ToDictionary();
        component.HealingMultipliers =
            state.HealingMultipliers
                .Select(multiplier
                    => (TryGetEntity(multiplier.Key, out var ne) ? ne.Value : EntityUid.Invalid, multiplier.Value))
                .ToDictionary();

        if (component.WoundableIntegrity != state.WoundableIntegrity)
        {
            var ev = new WoundableIntegrityChangedEvent(component.WoundableIntegrity, state.WoundableIntegrity);
            RaiseLocalEvent(uid, ref ev);

            if (_body.GetBody(uid) is {} body)
                UpdateMobAlerts(body);
        }

        component.WoundableIntegrity = state.WoundableIntegrity;

        if (component.WoundableSeverity != state.WoundableSeverity)
        {
            var ev = new WoundableSeverityChangedEvent(component.WoundableSeverity, state.WoundableSeverity);
            RaiseLocalEvent(uid, ref ev);
        }
        component.WoundableSeverity = state.WoundableSeverity;
    }

    private void UpdateMobAlerts(EntityUid body)
    {
        if (TryComp<MobStateComponent>(body, out var mob))
            _mobThreshold.UpdateAlerts(body, mob.CurrentState);
    }
}
