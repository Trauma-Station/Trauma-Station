// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.BlockTeleport;
using Content.Goobstation.Common.Physics;
using Content.Shared.Bed.Sleep;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;
using Content.Trauma.Shared.Heretic.Components.StatusEffects;
using Content.Trauma.Shared.Heretic.Events;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Cosmos;

public sealed class SharedStarTouchSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedStarMarkSystem _starMark = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedStarGazerSystem _starGazer = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly TouchSpellSystem _touchSpell = default!;

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
    }

    private void OnUseInHand(Entity<StarTouchComponent> ent, ref UseInHandEvent args)
    {
        var starGazer = _starGazer.ResolveStarGazer(args.User, out var spawned);
        if (starGazer == null)
            return;

        args.Handled = true;

        _touchSpell.InvokeTouchSpell(ent.Owner, args.User);

        if (spawned)
            return;

        _pulling.StopAllPulls(args.User);
        _transform.SetMapCoordinates(args.User, _transform.GetMapCoordinates(starGazer.Value.Owner));
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

        _pulling.StopAllPulls(target);

        var targetXform = Transform(target);
        var newCoords = Transform(heretic.Value).Coordinates;
        PredictedSpawnAtPosition(ent.Comp.CosmicCloud, targetXform.Coordinates);
        _transform.SetCoordinates((target, targetXform, MetaData(target)), newCoords);
        PredictedSpawnAtPosition(ent.Comp.CosmicCloud, newCoords);

        // Applying status effects next tick, otherwise status effects system shits itself
        Timer.Spawn(0,
            () =>
            {
                _status.TryUpdateStatusEffectDuration(target,
                    SleepingSystem.StatusEffectForcedSleeping,
                    ent.Comp.SleepTime);
                _starMark.TryApplyStarMark(target);
            });
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
            hereticComp.PathStage);

        if (!HasComp<StarMarkComponent>(target))
        {
            _starMark.TryApplyStarMark((target, mobState));
            return;
        }

        _status.TryRemoveStatusEffect(target, SharedStarMarkSystem.StarMarkStatusEffect);
        _status.TryUpdateStatusEffectDuration(target, DrowsinessStatusEffect, comp.DrowsinessTime);

        if (!_status.TryUpdateStatusEffectDuration(target, StarTouchStatusEffect, comp.Duration))
            return;

        EnsureComp<BlockTeleportComponent>(target);
        var beam = EnsureComp<ComplexJointVisualsComponent>(target);
        beam.Data[GetNetEntity(args.User)] = new ComplexJointVisualsData(StarTouchBeamDataId, comp.BeamSprite);
        Dirty(target, beam);
        var trail = EnsureComp<CosmicTrailComponent>(target);
        trail.CosmicFieldLifetime = comp.CosmicFieldLifetime;
        trail.Strength = hereticComp.PathStage;
    }
}
