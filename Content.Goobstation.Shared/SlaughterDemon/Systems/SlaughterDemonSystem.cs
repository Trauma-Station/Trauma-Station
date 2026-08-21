// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Devour;
using Content.Shared.Actions;
using Content.Shared.Administration.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Gibbing;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Polymorph;
using Content.Shared.Random.Helpers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.SlaughterDemon.Systems;

public sealed partial class SlaughterDemonSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _speed = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private RejuvenateSystem _rejuvenate = default!;
    [Dependency] private SlaughterDevourSystem _devour = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private EntityQuery<MobStateComponent> _mobQuery = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SlaughterDemonComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.Accumulator || !comp.ExitedBloodCrawl)
                continue;

            comp.ExitedBloodCrawl = false;
            Dirty(uid, comp);
            _speed.RefreshMovementSpeedModifiers(uid);
        }
    }

    [SubscribeLocalEvent]
    private void OnPolymorph(Entity<SlaughterDemonComponent> ent, ref PolymorphedEvent args)
    {
        if (!TryComp<SlaughterDevourComponent>(args.NewEntity, out var component)
            || component.Container == null)
            return;

        ent.Comp.ConsumedMobs.RemoveAll(uid => TerminatingOrDeleted(uid));
        foreach (var entity in ent.Comp.ConsumedMobs)
        {
            _container.Insert(entity, component.Container);
        }

        // Cooldown
        foreach (var action in _actions.GetActions(args.NewEntity))
            _actions.StartUseDelay(action.Owner);
    }

    [SubscribeLocalEvent]
    private void OnBloodCrawlExit(Entity<SlaughterDemonComponent> ent, ref BloodCrawlExitEvent args)
    {
        ent.Comp.Accumulator = _timing.CurTime + ent.Comp.NextUpdate;
        ent.Comp.ExitedBloodCrawl = true;
        Dirty(ent);

        _speed.RefreshMovementSpeedModifiers(ent.Owner);

        PlayMeatySound(ent);
        PredictedSpawnAtPosition(ent.Comp.JauntUpEffect, Transform(ent.Owner).Coordinates);
    }

    [SubscribeLocalEvent]
    private void OnSlaughterDevour(Entity<SlaughterDemonComponent> ent, ref SlaughterDevourDoAfterEvent args)
    {
        if (args.Cancelled || args.Target is not { } target)
            return;

        var (uid, comp) = ent;
        comp.ConsumedMobs.Add(target);
        comp.Devoured++;
        Dirty(ent);

        if (!TryComp<SlaughterDevourComponent>(uid, out var devour) ||
            devour.Container is not { } container)
            return;

        var attemptEv = new SlaughterDevourAttemptEvent(target, uid);
        RaiseLocalEvent(target, ref attemptEv);

        if (attemptEv.Cancelled)
            return;

        var coords = Transform(target).Coordinates;
        _container.Insert(target, container);

        // Stop them from being able to self-revive
        EnsureComp<PreventSelfRevivalComponent>(target);

        // Kill them for sure, just in case
        if (_mobQuery.TryComp(target, out var mob))
            _mob.ChangeMobState(target, MobState.Dead, mob);

        _bloodstream.SpillAllSolutions(target);

        _audio.PlayPredicted(devour.FeastSound, coords, uid);

        _devour.HealAfterDevouring((uid, devour), target);
        _devour.IncrementObjective(ent, target);
    }

    [SubscribeLocalEvent]
    private void OnBeingGibbed(Entity<SlaughterDemonComponent> ent, ref BeingGibbedEvent args)
    {
        if (!TryComp<SlaughterDevourComponent>(ent.Owner, out var devour)
            || devour.Container == null)
            return;

        _container.EmptyContainer(devour.Container);

        // Allow everyone to self revive again (if they have the ability to)
        foreach (var entity in ent.Comp.ConsumedMobs)
            RemComp<PreventSelfRevivalComponent>(entity);

        // heal them if they were in the laughter demon
        if (!ent.Comp.IsLaughter)
            return;

        foreach (var entity in ent.Comp.ConsumedMobs)
            _rejuvenate.PerformRejuvenate(entity);
    }

    [SubscribeLocalEvent]
    private void RefreshMovement(Entity<SlaughterDemonComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.ExitedBloodCrawl)
        {
            args.ModifySpeed(ent.Comp.SpeedModWalk, ent.Comp.SpeedModRun);
        }
    }

    [SubscribeLocalEvent]
    private void OnBloodCrawlAttempt(Entity<SlaughterDemonComponent> ent, ref BloodCrawlAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        PredictedSpawnAtPosition(ent.Comp.JauntEffect, Transform(ent.Owner).Coordinates);
    }

    [SubscribeLocalEvent]
    private void OnPickup(Entity<SlaughterDemonComponent> ent, ref PickupAttemptEvent args)
    {
        args.Cancel();
    }

    #region Helper

    private void PlayMeatySound(Entity<SlaughterDemonComponent> ent)
    {
        if (!SharedRandomExtensions.PredictedProb(_timing, ent.Comp.BloodCrawlSoundChance, GetNetEntity(ent)))
            return;

        // ALEXA PLAY MEATY SOUND 🔊🔊
        var parm = ent.Comp.BloodCrawlSounds.Params // chicken parm
            .WithMaxDistance(ent.Comp.BloodCrawlSoundLookup);
        _audio.PlayPredicted(ent.Comp.BloodCrawlSounds, ent, ent, parm);
    }

    #endregion
}
