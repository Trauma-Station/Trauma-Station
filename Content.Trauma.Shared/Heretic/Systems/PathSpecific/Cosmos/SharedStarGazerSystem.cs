// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Heretic.Systems.Abilities;
using Content.Trauma.Shared.Physics.ComplexJoint;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Cosmos;

public abstract partial class SharedStarGazerSystem : EntitySystem
{
    [Dependency] protected StatusEffectsSystem Status = default!;
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected SharedTransformSystem Xform = default!;

    [Dependency] private SharedHereticAbilitySystem _hereticAbility = default!;
    [Dependency] private SharedHereticSystem _heretic = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedStarMarkSystem _starMark = default!;
    [Dependency] private SharedContinuousBeamSystem _beam = default!;

    public const string JointId = "stargaze";

    [SubscribeLocalEvent]
    private void OnStarGazerAttackAttempt(Entity<StarGazerComponent> ent, ref AttackAttemptEvent args)
    {
        if (Status.HasStatusEffect(ent, ent.Comp.InactiveStatus))
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnResolveStarGazer(Entity<HereticComponent> ent, ref EventHereticResolveStarGazer args)
    {
        if (!TryComp(ent, out MindComponent? mind) || mind.OwnedEntity is not { } uid)
            return;

        ResolveStarGazer(uid, out _, false);
    }

    [SubscribeLocalEvent]
    private void OnStarGazerHit(Entity<StarGazerComponent> ent, ref MeleeHitEvent args)
    {
        foreach (var uid in args.HitEntities)
        {
            _starMark.TryApplyStarMark(uid);
        }
    }

    [SubscribeLocalEvent]
    private void OnStarGazeAttackAttempt(Entity<StarGazeComponent> ent, ref AttackAttemptEvent args)
    {
        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnStarGazeShutdown(Entity<StarGazeComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent))
            _movement.RefreshMovementSpeedModifiers(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnStarGazeStartup(Entity<StarGazeComponent> ent, ref ComponentStartup args)
    {
        _movement.RefreshMovementSpeedModifiers(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnRefreshMovespeed(Entity<StarGazeComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.LifeStage < ComponentLifeStage.Stopping)
            args.ModifySpeed(ent.Comp.Slowdown.X, ent.Comp.Slowdown.Y, true);
    }

    [SubscribeLocalEvent]
    private void OnStarGazeDoAfter(Entity<StarGazeComponent> ent, ref StarGazeDoAfterEvent args)
    {
        var (uid, comp) = ent;

        if (!TryComp(uid, out ContinuousBeamGunComponent? gun))
            return;

        if (args.Cancelled || args.Handled || gun.CursorPosition == null)
        {
            if (TryGetEntity(args.OrbEffect, out var orb) && Exists(orb.Value))
                PredictedQueueDel(orb.Value);

            RemCompDeferred(uid, comp);
            return;
        }

        var coords = Xform.GetMapCoordinates(uid);

        if (gun.CursorPosition.Value.MapId != coords.MapId)
        {
            RemCompDeferred(uid, comp);
            return;
        }

        if (_beam.ShootLaser(uid, uid, Xform.ToCoordinates(coords)) == null)
        {
            RemCompDeferred(uid, comp);
            return;
        }

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnStarGaze(Entity<StarGazerComponent> ent, ref StarGazeEvent args)
    {
        if (!_hereticAbility.TryUseAbility(args, false))
            return;

        var orbEffect = PredictedSpawnAttachedTo(args.OrbEffect, ent.Owner.ToCoordinates());

        var doArgs = new DoAfterArgs(EntityManager,
            ent,
            args.DoAfterDelay,
            new StarGazeDoAfterEvent(GetNetEntity(orbEffect)),
            ent)
        {
            BreakOnHandChange = false,
            RequireCanInteract = false,
            MultiplyDelay = false,
        };

        if (!_doAfter.TryStartDoAfter(doArgs))
        {
            PredictedQueueDel(orbEffect);
            return;
        }

        EnsureComp<StarGazeComponent>(ent);
        _audio.PlayPredicted(args.BeamStartSound, ent, ent);

        args.Handled = true;
    }

    public Entity<StarGazerComponent>? ResolveStarGazer(Entity<CosmosPassiveComponent?> summoner,
        out bool spawned,
        bool checkAscend = true,
        EntityCoordinates? spawnCoords = null)
    {
        spawned = false;

        if (!Resolve(summoner, ref summoner.Comp, false) ||
            !_heretic.TryGetHereticComponent(summoner.Owner, out var heretic, out var mind) ||
            heretic.CurrentPath != HereticPath.Cosmos || checkAscend && !heretic.Ascended)
            return null;

        StarGazerComponent? comp;
        Components.Ghoul.HereticMinionComponent? minion;

        var starGazer = summoner.Comp.StarGazer;
        if (!Exists(starGazer))
            starGazer = heretic.Minions.FirstOrNull(x => Exists(x) && HasComp<StarGazerComponent>(x));

        if (starGazer == null)
        {
            starGazer = PredictedSpawnAtPosition(summoner.Comp.StarGazerId,
                spawnCoords ?? Transform(summoner).Coordinates);
            Xform.AttachToGridOrMap(starGazer.Value);
            comp = EnsureComp<StarGazerComponent>(starGazer.Value);
            minion = EnsureComp<Components.Ghoul.HereticMinionComponent>(starGazer.Value);
            minion.MinionId = GetNetEntity(mind).Id;
            minion.BoundHeretic = summoner;
            summoner.Comp.StarGazer = starGazer.Value;
            heretic.Minions.Add(starGazer.Value);
            Dirty(mind, heretic);
            Dirty(summoner, summoner.Comp);
            Dirty(starGazer.Value, minion);
            spawned = true;
            return (starGazer.Value, comp);
        }

        heretic.Minions.Add(starGazer.Value);
        Dirty(mind, heretic);

        comp = EnsureComp<StarGazerComponent>(starGazer.Value);

        if (EnsureComp<Components.Ghoul.HereticMinionComponent>(starGazer.Value, out minion) &&
            minion.BoundHeretic == summoner.Owner)
            return (starGazer.Value, comp);

        minion.MinionId = GetNetEntity(mind).Id;
        minion.BoundHeretic = summoner.Owner;
        Dirty(starGazer.Value, minion);

        return (starGazer.Value, comp);
    }
}
