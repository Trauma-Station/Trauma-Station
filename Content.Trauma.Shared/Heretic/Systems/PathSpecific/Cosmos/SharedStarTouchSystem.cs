// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.BlockTeleport;
using Content.Goobstation.Common.Physics;
using Content.Shared.Bed.Sleep;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;
using Content.Trauma.Shared.Heretic.Components.StatusEffects;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Teleportation;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Cosmos;

public sealed partial class SharedStarTouchSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedStarMarkSystem _starMark = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private SharedStarGazerSystem _starGazer = default!;
    [Dependency] private SharedHereticSystem _heretic = default!;
    [Dependency] private TeleportSystem _teleport = default!;
    [Dependency] private TouchSpellSystem _touchSpell = default!;
    [Dependency] private BlindableSystem _blind = default!;

    public static readonly EntProtoId StarTouchStatusEffect = "StatusEffectStarTouched";
    public static readonly EntProtoId DrowsinessStatusEffect = "StatusEffectDrowsiness";
    public const string StarTouchBeamDataId = "startouch";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StarTouchComponent, TouchSpellUsedEvent>(OnTouchSpell);
        SubscribeLocalEvent<StarTouchComponent, UseInHandEvent>(OnUseInHand);

        SubscribeLocalEvent<StarTouchedStatusEffectComponent, StatusEffectAppliedEvent>(OnApply);
        SubscribeLocalEvent<StarTouchedStatusEffectComponent, StatusEffectRemovedEvent>(OnRemove);

        // TODO remove this when TemporaryBlindness new status effect refactor is real
        SubscribeLocalEvent<StarTouchedComponent, CanSeeAttemptEvent>(OnCanSee);
        SubscribeLocalEvent<StarTouchedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StarTouchedComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<StarTouchedComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        _blind.UpdateIsBlind(ent.Owner);
    }

    private void OnStartup(Entity<StarTouchedComponent> ent, ref ComponentStartup args)
    {
        _blind.UpdateIsBlind(ent.Owner);
    }

    private void OnCanSee(Entity<StarTouchedComponent> ent, ref CanSeeAttemptEvent args)
    {
        if (ent.Comp.LifeStage > ComponentLifeStage.Running)
            return;

        args.Cancel();
    }

    private void OnUseInHand(Entity<StarTouchComponent> ent, ref UseInHandEvent args)
    {
        var user = args.User;
        if (_starGazer.ResolveStarGazer(user, out var spawned) is not { } starGazer)
            return;

        args.Handled = true;

        _touchSpell.InvokeTouchSpell(ent.Owner, user);

        if (spawned || TerminatingOrDeleted(starGazer))
            return;

        var coords = Transform(starGazer).Coordinates;
        _teleport.Teleport(user, coords, user: user);
    }

    private void OnRemove(Entity<StarTouchedStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        var target = args.Target;

        if (TerminatingOrDeleted(target))
            return;

        RemCompDeferred<BlockTeleportComponent>(target);
        RemCompDeferred<StarTouchedComponent>(target);
        RemCompDeferred<CosmicTrailComponent>(target);

        if (!TryComp(target, out ComplexJointVisualsComponent? joint))
            return;

        EntityUid? heretic = null;
        List<NetEntity> toRemove = new();
        foreach (var (netEnt, data) in joint.Data)
        {
            if (data.Id != StarTouchBeamDataId)
                continue;

            toRemove.Add(netEnt);

            if (!TryGetEntity(netEnt, out var entity) || TerminatingOrDeleted(entity))
                continue;

            heretic = entity;
        }

        if (toRemove.Count == joint.Data.Count)
            RemCompDeferred(target, joint);
        else if (toRemove.Count != 0)
        {
            foreach (var netEnt in toRemove)
            {
                joint.Data.Remove(netEnt);
            }

            Dirty(target, joint);
        }

        if (heretic == null || !TryComp(ent, out StatusEffectComponent? status) || status.EndEffectTime == null ||
            status.EndEffectTime > _timing.CurTime)
            return;

        var targetXform = Transform(target);
        var newCoords = Transform(heretic.Value).Coordinates;
        PredictedSpawnAtPosition(ent.Comp.CosmicCloud, targetXform.Coordinates);
        _teleport.Teleport(target, newCoords, force: true);
        PredictedSpawnAtPosition(ent.Comp.CosmicCloud, newCoords);

        var delay = TimeSpan.FromMilliseconds(100);
        _status.TryUpdateStatusEffectDuration(target,
            SleepingSystem.StatusEffectForcedSleeping,
            ent.Comp.SleepTime, delay);
        _starMark.TryApplyStarMark(target, delay);
    }

    private void OnApply(Entity<StarTouchedStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        EnsureComp<StarTouchedComponent>(args.Target);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<StarTouchedComponent>();
        while (query.MoveNext(out var uid, out var touch))
        {
            touch.Accumulator += frameTime;

            if (touch.Accumulator < touch.TickInterval)
                continue;

            touch.Accumulator = 0f;

            UpdateBeams((uid, touch));
        }
    }

    private void UpdateBeams(Entity<StarTouchedComponent, ComplexJointVisualsComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2, false))
            return;

        var hasStarBeams = false;

        foreach (var (netEnt, _) in ent.Comp2.Data.Where(x => x.Value.Id == StarTouchBeamDataId).ToList())
        {
            if (!TryGetEntity(netEnt, out var target) || TerminatingOrDeleted(target) ||
                !_transform.InRange(target.Value, ent.Owner, ent.Comp1.Range))
            {
                ent.Comp2.Data.Remove(netEnt);
                continue;
            }

            hasStarBeams = true;
        }

        Dirty(ent.Owner, ent.Comp2);

        if (hasStarBeams)
            return;

        _status.TryRemoveStatusEffect(ent, StarTouchStatusEffect);
    }

    private void OnTouchSpell(Entity<StarTouchComponent> ent, ref TouchSpellUsedEvent args)
    {
        var target = args.Target;
        var comp = ent.Comp;

        if (!TryComp(target, out MobStateComponent? mobState))
            return;

        args.Invoke = true;

        if (!_heretic.TryGetHereticComponent(args.User, out var hereticComp, out _) ||
            _heretic.TryGetHereticComponent(target, out var th, out _) && th.CurrentPath == HereticPath.Cosmos)
            return;

        var range = hereticComp.Ascended ? 2 : 1;
        var xform = Transform(args.User);
        _starMark.SpawnCosmicFieldLine(xform.Coordinates,
            Angle.FromDegrees(90f).RotateDir(xform.LocalRotation.GetDir()).AsFlag(),
            -range,
            range,
            0,
            hereticComp.PassiveLevel);

        if (!HasComp<StarMarkComponent>(target))
        {
            _starMark.TryApplyStarMark((target, mobState));
            return;
        }

        _status.TryRemoveStatusEffect(target, SharedStarMarkSystem.StarMarkStatusEffect);
        _status.TryUpdateStatusEffectDuration(target, DrowsinessStatusEffect, comp.DrowsinessTime);

        if (!_status.TryUpdateStatusEffectDuration(target, StarTouchStatusEffect, out var effect, comp.Duration))
            return;

        var effectComp = EnsureComp<StarTouchedStatusEffectComponent>(effect.Value);
        effectComp.User = args.User;
        Dirty(effect.Value, effectComp);

        EnsureComp<BlockTeleportComponent>(target);
        var beam = EnsureComp<ComplexJointVisualsComponent>(target);
        beam.Data[GetNetEntity(args.User)] = new ComplexJointVisualsData(StarTouchBeamDataId, comp.BeamSprite);
        Dirty(target, beam);
        var trail = EnsureComp<CosmicTrailComponent>(target);
        trail.CosmicFieldLifetime = comp.CosmicFieldLifetime;
        trail.Strength = hereticComp.PassiveLevel;
    }
}
