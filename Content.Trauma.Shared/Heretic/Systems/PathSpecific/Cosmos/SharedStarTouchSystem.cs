// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.BlockTeleport;
using Content.Shared.Bed.Sleep;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;
using Content.Trauma.Shared.Heretic.Components.StatusEffects;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Physics.ComplexJoint;
using Content.Trauma.Shared.Teleportation;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Cosmos;

public sealed partial class SharedStarTouchSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private SharedComplexJointVisualsSystem _joint = default!;
    [Dependency] private SharedStarMarkSystem _starMark = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private SharedStarGazerSystem _starGazer = default!;
    [Dependency] private SharedHereticSystem _heretic = default!;
    [Dependency] private TeleportSystem _teleport = default!;
    [Dependency] private TouchSpellSystem _touchSpell = default!;

    public static readonly EntProtoId StarTouchStatusEffect = "StatusEffectStarTouched";
    public static readonly EntProtoId DrowsinessStatusEffect = "StatusEffectDrowsiness";
    public const string StarTouchBeamDataId = "startouch";

    [SubscribeLocalEvent]
    private void UpdateBeams(Entity<StarTouchedComponent> ent, ref ComplexJointUpdateEvent args)
    {
        if (args.UpdatedIds.ContainsKey(StarTouchBeamDataId))
            return;

        _status.TryRemoveStatusEffect(ent, StarTouchStatusEffect);
    }

    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
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
            ent.Comp.SleepTime,
            delay);
        _starMark.TryApplyStarMark(target, delay);
    }

    [SubscribeLocalEvent]
    private void OnApply(Entity<StarTouchedStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        EnsureComp<StarTouchedComponent>(args.Target);
    }

    [SubscribeLocalEvent]
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
        var data = new ComplexJointVisualsData(StarTouchBeamDataId, comp.BeamSprite, comp.Range)
        {
            ShouldCollide = false,
            ReverseBeam = true,
        };
        _joint.CreateJoint(args.User, target, data);
        var trail = EnsureComp<CosmicTrailComponent>(target);
        trail.CosmicFieldLifetime = comp.CosmicFieldLifetime;
        trail.Strength = hereticComp.PassiveLevel;
    }
}
