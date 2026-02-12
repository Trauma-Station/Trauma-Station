// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.CCVar;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Consciousness;
using Content.Medical.Shared.Pain;
using Content.Medical.Shared.Traumas;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Jittering;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Random;
using System.Linq;
using Content.Shared.FixedPoint;

namespace Content.Medical.Shared.Pain;

public sealed partial class PainSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly BodyStatusSystem _bodyStatus = default!;

    [Dependency] private readonly SharedAudioSystem _IHaveNoMouthAndIMustScream = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    [Dependency] private readonly MobStateSystem _mobState = default!;

    [Dependency] private readonly StandingStateSystem _standing = default!;

    [Dependency] private readonly WoundSystem _wound = default!;
    [Dependency] private readonly ConsciousnessSystem _consciousness = default!;
    [Dependency] private readonly TraumaSystem _trauma = default!;

    private bool _screamsEnabled = false;
    private float _screamChance = 0.20f;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NerveComponent, ComponentHandleState>(OnComponentHandleState);
        SubscribeLocalEvent<NerveComponent, ComponentGetState>(OnComponentGet);

        SubscribeLocalEvent<NerveComponent, OrganGotInsertedEvent>(OnNerveInserted);
        SubscribeLocalEvent<NerveComponent, OrganGotRemovedEvent>(OnNerveRemoved);

        SubscribeLocalEvent<NerveSystemComponent, MobStateChangedEvent>(OnMobStateChanged);

        _screamsEnabled = _cfg.GetCVar(SurgeryCVars.PainScreams);
        _screamChance = _cfg.GetCVar(SurgeryCVars.PainScreamChance);

        InitAffliction();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _painJobQueue.Process();

        if (!_timing.IsFirstTimePredicted)
            return;

        // Process pain decay for all entities with active decay
        var decayQuery = EntityQueryEnumerator<PainDecayComponent, NerveSystemComponent>();
        while (decayQuery.MoveNext(out var uid, out var decay, out var nerveSystem))
        {
            if (TerminatingOrDeleted(uid))
                continue;

            UpdatePainDecay(uid, decay, nerveSystem);
        }

        // Process regular pain updates
        using var query = EntityQueryEnumerator<NerveSystemComponent>();
        while (query.MoveNext(out var ent, out var nerveSystem))
        {
            if (TerminatingOrDeleted(ent))
                continue;

            _painJobQueue.EnqueueJob(new PainTimerJob(this, (ent, nerveSystem), PainJobTime));
        }
    }

    private void OnComponentHandleState(EntityUid uid, NerveComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not NerveComponentState state)
            return;

        var parentEntity = GetEntity(state.ParentedNerveSystem);
        component.ParentedNerveSystem = !TerminatingOrDeleted(parentEntity) ? parentEntity : EntityUid.Invalid;
        component.PainMultiplier = state.PainMultiplier;

        component.PainFeelingModifiers.Clear();
        foreach (var ((modEntity, id), modifier) in state.PainFeelingModifiers)
        {
            var entity = GetEntity(modEntity);
            if (!TerminatingOrDeleted(entity) && !component.PainFeelingModifiers.ContainsKey((entity, id)))
                component.PainFeelingModifiers.Add((entity, id), modifier);
        }
    }

    private void OnComponentGet(EntityUid uid, NerveComponent comp, ref ComponentGetState args)
    {
        var state = new NerveComponentState();

        if (!TerminatingOrDeleted(comp.ParentedNerveSystem))
            state.ParentedNerveSystem = GetNetEntity(comp.ParentedNerveSystem);
        state.PainMultiplier = comp.PainMultiplier;

        foreach (var ((modEntity, id), modifier) in comp.PainFeelingModifiers)
        {
            if (!TerminatingOrDeleted(modEntity))
                state.PainFeelingModifiers.Add((GetNetEntity(modEntity), id), modifier);
        }

        args.State = state;
    }

    private void OnNerveInserted(Entity<NerveComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (!_consciousness.TryGetNerveSystem(args.Target, out var brainUid) || TerminatingOrDeleted(brainUid.Value))
            return;

        UpdateNerveSystemNerves(brainUid.Value, args.Target, brainUid.Value.Comp);
    }

    private void OnNerveRemoved(Entity<NerveComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (!_consciousness.TryGetNerveSystem(args.Target, out var brainUid) || TerminatingOrDeleted(brainUid.Value))
            return;

        var nerves = brainUid.Value.Comp;
        foreach (var modifier in brainUid.Value.Comp.Modifiers
                     .Where(modifier => modifier.Key.Item1 == ent.Owner))
        {
            nerves.Modifiers.Remove((modifier.Key.Item1, modifier.Key.Item2));
        }

        UpdateNerveSystemNerves(brainUid.Value, args.Target, nerves);
    }

    private void OnMobStateChanged(Entity<NerveSystemComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.Target != ent.Owner)
            return;

        switch (args.NewMobState)
        {
            case MobState.Critical:
                var sex = Sex.Unsexed;
                if (TryComp<HumanoidProfileComponent>(ent, out var humanoid))
                    sex = humanoid.Sex;

                PlayPainSoundWithCleanup(ent, ent.Comp, ent.Comp.CritWhimpers[sex], AudioParams.Default.WithVolume(-12f));
                ent.Comp.NextCritScream = _timing.CurTime + _random.Next(ent.Comp.CritScreamsIntervalMin, ent.Comp.CritScreamsIntervalMax);
                break;

            case MobState.Dead:
                CleanupSounds(ent.Comp);
                break;
        }
    }

    private void UpdateNerveSystemNerves(EntityUid uid, EntityUid body, NerveSystemComponent component)
    {
        component.Nerves.Clear();
        foreach (var organ in _body.GetOrgans(body))
        {
            if (organ.Comp.Category is not {} category || !TryComp<NerveComponent>(organ, out var nerve))
                continue;

            component.Nerves.Add(organ.Owner, nerve);

            nerve.ParentedNerveSystem = uid;
            Dirty(organ, nerve);
        }
        Dirty(uid, component);
    }

    #region Pain Decay

    /// <summary>
    /// Starts pain decay for a nerve system
    /// </summary>

    public void StartPainDecay(EntityUid uid, FixedPoint2 initialPain, TimeSpan decayDuration, NerveSystemComponent? nerveSystem = null)
    {
        if (!Resolve(uid, ref nerveSystem, false))
            return;

        // Remove any existing decay
        if (TryComp<PainDecayComponent>(uid, out var existingDecay))
        {
            // If the new decay would be longer than remaining time, keep the existing one
            var remainingTime = (existingDecay.StartTime + existingDecay.DecayDuration) - _timing.CurTime;
            if (remainingTime > decayDuration)
                return;

            RemComp<PainDecayComponent>(uid);
        }

        var decay = EnsureComp<PainDecayComponent>(uid);
        decay.InitialPain = initialPain;
        decay.StartTime = _timing.CurTime;
        decay.DecayDuration = decayDuration;
        decay.NerveSystemUid = uid;
        Dirty(uid, decay);
    }

    // Stops any active pain decay for an entity
    public void StopPainDecay(EntityUid uid)
    {
        if (HasComp<PainDecayComponent>(uid))
            RemComp<PainDecayComponent>(uid);
    }

    // Updates the pain value based on decay progress
    private void UpdatePainDecay(EntityUid uid, PainDecayComponent decay, NerveSystemComponent nerveSystem)
    {
        var elapsed = _timing.CurTime - decay.StartTime;

        // If decay duration has passed, set pain to 0 and remove decay component
        if (elapsed >= decay.DecayDuration)
        {
            nerveSystem.Pain = FixedPoint2.Zero;
            Dirty(uid, nerveSystem);
            RemComp<PainDecayComponent>(uid);
            return;
        }

        // Calculate current pain based on decay progress
        var progress = (float)(elapsed.TotalSeconds / decay.DecayDuration.TotalSeconds);
        var currentPain = decay.InitialPain * (1 - progress);

        // Only update if pain would decrease
        if (currentPain < nerveSystem.Pain)
        {
            nerveSystem.Pain = currentPain;
            Dirty(uid, nerveSystem);
        }
    }

    #endregion
}
