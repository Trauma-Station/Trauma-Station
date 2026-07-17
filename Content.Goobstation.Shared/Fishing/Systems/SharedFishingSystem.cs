// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Fishing.Components;
using Content.Goobstation.Shared.Fishing.Events;
using Content.Shared.Actions;
using Content.Shared.EntityTable;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Throwing;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Goobstation.Shared.Fishing.Systems;

/// <summary>
/// This handles... da fish
/// </summary>
public abstract partial class SharedFishingSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityTableSystem _table = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private EntityQuery<FishingSpotComponent> _spotQuery = default!;
    [Dependency] private EntityQuery<ActiveFisherComponent> _fisherQuery;
    [Dependency] private EntityQuery<ActiveFishingRodComponent> _activeRodQuery;
    [Dependency] private EntityQuery<FishingRodComponent> _rodQuery;
    [Dependency] private EntityQuery<FishingLureComponent> _lureQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FishingRodComponent, MapInitEvent>(OnFishingRodInit);
        SubscribeLocalEvent<FishingRodComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<FishingRodComponent, ThrowFishingLureActionEvent>(OnThrowFloat);
        SubscribeLocalEvent<FishingRodComponent, PullFishingLureActionEvent>(OnPullLure);
        SubscribeLocalEvent<FishingRodComponent, EntParentChangedMessage>(OnRodParentChanged);
        SubscribeLocalEvent<FishingRodComponent, UseInHandEvent>(OnRodUseInHand);

        SubscribeLocalEvent<FishingRodComponent, EntityTerminatingEvent>(OnRodTerminating);
        SubscribeLocalEvent<FishingLureComponent, EntityTerminatingEvent>(OnLureTerminating);

        SubscribeLocalEvent<FishingLureComponent, StartCollideEvent>(OnLureCollide);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted || _timing.ApplyingState)
            return;

        var now = _timing.CurTime;
        var activeFishers = EntityQueryEnumerator<ActiveFisherComponent>();
        while (activeFishers.MoveNext(out var fisher, out var fisherComp))
        {
            var rod = fisherComp.FishingRod;
            if (!_rodQuery.TryComp(rod, out var rodComp) ||
                rodComp.FishingLure is not { } lure ||
                !_activeRodQuery.TryComp(rod, out var active))
            {
                RemCompDeferred(fisher, fisherComp);
                continue;
            }

            // Fish fighting logic
            CalculateFightingTimings((fisher, fisherComp), active);

            if (fisherComp.TotalProgress < 0)
            {
                // It's over
                _popup.PopupClient(Loc.GetString("fishing-progress-fail"), fisher, fisher);
                StopFishing((rod, rodComp), fisher);
            }
            else if (fisherComp.TotalProgress >= 1)
            {
                if (active.Fish is { } fish)
                {
                    ThrowFishReward(fish, lure, fisher);
                    _popup.PopupClient(Loc.GetString("fishing-progress-success"), fisher, fisher);
                }

                StopFishing((rod, rodComp), fisher);
            }
        }

        var fishingSpots = EntityQueryEnumerator<ActiveFishingRodComponent>();
        while (fishingSpots.MoveNext(out var rod, out var active))
        {
            if (active.Reeling || active.FishingStartTime == null || now < active.FishingStartTime)
                continue;

            // Trigger start of the fishing process
            if (!_rodQuery.TryComp(rod, out var comp) || TerminatingOrDeleted(active.Fisher))
            {
                RemCompDeferred(rod, active);
                continue;
            }

            var fisher = active.Fisher;
            var activeFisher = EnsureComp<ActiveFisherComponent>(fisher);
            activeFisher.FishingRod = rod;
            activeFisher.ProgressPerUse *= comp.Efficiency;
            activeFisher.TotalProgress = comp.StartingProgress;
            activeFisher.NextStruggle = now + comp.StartingStruggleTime; // Compensate for ping, give them a bit of time
            Dirty(fisher, activeFisher);

            _popup.PopupClient(Loc.GetString("fishing-progress-start"), fisher, fisher);
            active.Reeling = true;
        }

        var fishingLures = EntityQueryEnumerator<FishingLureComponent, TransformComponent>();
        while (fishingLures.MoveNext(out var fishingLure, out var lureComp, out var xform))
        {
            if (now < lureComp.NextUpdate)
                continue;

            lureComp.NextUpdate = now + lureComp.UpdateInterval;

            if (TerminatingOrDeleted(lureComp.FishingRod) ||
                !_rodQuery.TryComp(lureComp.FishingRod, out var fishingRodComp))
                continue;

            var lurePos = _transform.GetMapCoordinates(xform);
            var rodPos = _transform.GetMapCoordinates(lureComp.FishingRod);
            var distance = lurePos.Position - rodPos.Position;
            var fisher = Transform(lureComp.FishingRod).ParentUid;

            if (TerminatingOrDeleted(fisher) ||
                distance.Length() > fishingRodComp.BreakOnDistance ||
                lurePos.MapId != rodPos.MapId ||
                !_hands.IsHolding(fisher, lureComp.FishingRod) ||
                !HasComp<ActorComponent>(fisher))
            {
                var rod = (lureComp.FishingRod, fishingRodComp);
                StopFishing(rod, fisher);
                ToggleFishingActions(rod, fisher, false);
            }
        }
    }

    /// <summary>
    /// if AddPulling is true, we ADD Pulling action and REMOVE Throwing action.
    /// Basically true if we start, and false if we end.
    /// </summary>
    private void ToggleFishingActions(Entity<FishingRodComponent> ent, EntityUid fisher, bool addPulling)
    {
        if (TerminatingOrDeleted(ent) || TerminatingOrDeleted(fisher))
            return;

        if (addPulling)
        {
            _actions.RemoveAction(ent.Comp.ThrowLureActionEntity);
            _actions.AddAction(fisher, ref ent.Comp.PullLureActionEntity, ent.Comp.PullLureActionId, ent);
        }
        else
        {
            _actions.RemoveAction(ent.Comp.PullLureActionEntity);
            _actions.AddAction(fisher, ref ent.Comp.ThrowLureActionEntity, ent.Comp.ThrowLureActionId, ent);
        }
    }

    private void CalculateFightingTimings(Entity<ActiveFisherComponent> fisher, ActiveFishingRodComponent active)
    {
        if (_timing.CurTime < fisher.Comp.NextStruggle)
            return;

        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(fisher));
        fisher.Comp.NextStruggle = _timing.CurTime + TimeSpan.FromSeconds(rand.NextFloat(0.06f, 0.18f));
        fisher.Comp.TotalProgress -= active.FishDifficulty;
        Dirty(fisher);
    }

    /// <summary>
    /// Sets up fishing lure and throws it
    /// </summary>
    private void SpawnLure(Entity<FishingRodComponent> ent, EntityUid player, EntityCoordinates target)
    {
        var (uid, comp) = ent;
        var targetCoords = _transform.ToMapCoordinates(target);
        var playerCoords = _transform.GetMapCoordinates(player);

        var lure = PredictedSpawnNextToOrDrop(comp.FloatPrototype, player);
        comp.FishingLure = lure;
        Dirty(uid, comp);

        // Calculate throw direction
        var direction = targetCoords.Position - playerCoords.Position;
        if (direction == Vector2.Zero)
            direction = Vector2.UnitX; // If the user somehow manages to click directly in the center of themself, just toss it to the right i guess.

        // Yeet
        _throwing.TryThrow(lure, direction, 15f, player, 2f, null, true);

        // Set up lure component
        var lureComp = EnsureComp<FishingLureComponent>(lure);
        lureComp.FishingRod = uid;
        lureComp.Fisher = player;
        Dirty(lure, lureComp);

        // Rope visuals
        var visuals = EnsureComp<JointVisualsComponent>(lure);
        visuals.Sprite = comp.RopeSprite;
        visuals.OffsetA = comp.RopeLureOffset;
        visuals.OffsetB = comp.RopeUserOffset;
        visuals.Target = uid;
        Dirty(lure, visuals);
    }

    /// <summary>
    /// Spawns a fish and throws it to our player!
    /// </summary>
    private void ThrowFishReward(EntProtoId fishId, EntityUid lure, EntityUid target)
    {
        var position = Transform(lure).Coordinates;
        var fish = PredictedSpawnAtPosition(fishId, position);

        var ev = new FishCaughtEvent(fish, target);
        RaiseLocalEvent(fish, ref ev);

        // Throw da fish back to the player because it looks funny
        var direction = _transform.GetWorldPosition(target) - _transform.ToWorldPosition(position);
        if (direction == Vector2.Zero)
            return;

        var length = direction.Length();
        var distance = Math.Clamp(length, 0.5f, 15f);
        direction *= distance / length;

        _throwing.TryThrow(fish, direction, 7f);
    }

    /// <summary>
    /// Reels the fishing rod back and stops fishing progress if arguments are passed to it.
    /// </summary>
    private void StopFishing(Entity<FishingRodComponent?> ent, EntityUid? fisher)
    {
        if (!_rodQuery.Resolve(ent, ref ent.Comp, false))
            return;

        RemComp<ActiveFishingRodComponent>(ent);

        PredictedQueueDel(ent.Comp.FishingLure);

        if (_fisherQuery.TryComp(fisher, out var fisherComp))
        {
            RemComp(fisher.Value, fisherComp);

            ToggleFishingActions((ent, ent.Comp), fisher.Value, false);
        }

        ent.Comp.FishingLure = null;
        Dirty(ent, ent.Comp);
    }

    #region Terminating Events

    private void OnRodTerminating(Entity<FishingRodComponent> ent, ref EntityTerminatingEvent args)
    {
        StopFishing(ent.AsNullable(), fisher: Transform(ent).ParentUid);
    }

    private void OnLureTerminating(Entity<FishingLureComponent> ent, ref EntityTerminatingEvent args)
    {
        StopFishing(ent.Comp.FishingRod, ent.Comp.Fisher);
    }

    #endregion

    #region Event Handling

    private void OnThrowFloat(Entity<FishingRodComponent> ent, ref ThrowFishingLureActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        if (ent.Comp.FishingLure != null || !_transform.IsValid(args.Target))
            return;

        var player = args.Performer;
        SpawnLure(ent, player, args.Target);
        ToggleFishingActions(ent, player, true);
    }

    private void OnPullLure(Entity<FishingRodComponent> ent, ref PullFishingLureActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var player = args.Performer;
        var (uid, comp) = ent;

        if (comp.FishingLure == null)
        {
            ToggleFishingActions(ent, player, true);
            return;
        }

        _popup.PopupClient(Loc.GetString("fishing-rod-remove-lure", ("ent", Name(uid))), uid, uid);

        if (!_lureQuery.TryComp(comp.FishingLure, out var lureComp))
            return;

        if (lureComp.AttachedEntity is { } attachedEnt && Exists(attachedEnt))
        {
            // TODO: so this kinda just lets you pull anything right up to you, it should instead just apply an impulse in your direction modfiied by the weight of the player vs the object
            // Also we need to autoreel/snap the line if the player gets too far away
            // Also we should probably PVS override the lure if the rod is in PVS, and vice versa to stop the joint visuals from popping in/out
            var targetCoords = _transform.GetMapCoordinates(attachedEnt);
            var playerCoords = _transform.GetMapCoordinates(player);
            var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(uid));

            // Calculate throw direction
            var direction = (playerCoords.Position - targetCoords.Position) * rand.NextFloat(0.2f, 0.85f);

            // Yeet
            _throwing.TryThrow(attachedEnt, direction, 4f, player);
        }

        StopFishing(ent.AsNullable(), player);
        ToggleFishingActions(ent, player, false);
    }

    private void OnFishingRodInit(Entity<FishingRodComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.ThrowLureActionEntity, ent.Comp.ThrowLureActionId);
    }

    private void OnRodParentChanged(Entity<FishingRodComponent> ent, ref EntParentChangedMessage args)
    {
        if (TerminatingOrDeleted(ent) || !Exists(args.Transform.ParentUid))
            return;

        // Anything that is an active fisher should be fine.
        if (!_fisherQuery.HasComp(args.Transform.ParentUid))
        {
            StopFishing(ent.AsNullable(), args.OldParent);
        }
    }

    private void OnGetActions(Entity<FishingRodComponent> ent, ref GetItemActionsEvent args)
    {
        if (ent.Comp.FishingLure == null)
            args.AddAction(ref ent.Comp.ThrowLureActionEntity, ent.Comp.ThrowLureActionId);
        else
            args.AddAction(ref ent.Comp.PullLureActionEntity, ent.Comp.PullLureActionId);
    }

    private void OnRodUseInHand(Entity<FishingRodComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !_fisherQuery.TryComp(args.User, out var fisherComp))
            return;

        args.Handled = true;
        if (!_timing.IsFirstTimePredicted)
            return;

        fisherComp.TotalProgress += fisherComp.ProgressPerUse * ent.Comp.Efficiency;
        Dirty(args.User, fisherComp);
    }

    private void OnLureCollide(Entity<FishingLureComponent> ent, ref StartCollideEvent args)
    {
        // TODO: make it so this can collide with any unacnchored objects (items, mobs, etc) but not the player casting it (get parent of rod?)
        // Fishing spot logic
        var attachedEnt = args.OtherEntity;

        if (!_spotQuery.TryComp(attachedEnt, out var spotComp))
        {
            if (args.OtherBody.BodyType == BodyType.Static)
                return;

            Anchor(ent, attachedEnt);
            return;
        }

        // Anchor fishing float on an entity
        Anchor(ent, attachedEnt);

        // Currently we don't support multiple loots from this
        var fish = _table.GetSpawns(spotComp.FishList).First();

        // Get fish difficulty
        ProtoMan.Index(fish).TryGetComponent(out FishComponent? fishComp, Factory);

        // Assign things that depend on the fish
        var rod = ent.Comp.FishingRod;
        var active = EnsureComp<ActiveFishingRodComponent>(rod);
        active.Fish = fish;
        active.FishDifficulty = fishComp?.FishDifficulty ?? FishComponent.DefaultDifficulty;

        // Assign things that depend on the spot
        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(rod));
        var time = spotComp.FishDefaultTimer + rand.NextFloat(-spotComp.FishTimerVariety, spotComp.FishTimerVariety);
        active.FishingStartTime = _timing.CurTime + TimeSpan.FromSeconds(time);
        active.Fisher = ent.Comp.Fisher;
        Dirty(rod, active);
    }

    #endregion

    private void Anchor(Entity<FishingLureComponent> ent, EntityUid attachedEnt)
    {
        var spotPosition = _transform.GetWorldPosition(attachedEnt);
        _transform.SetWorldPosition(ent, spotPosition);
        _transform.SetParent(ent, attachedEnt);
        _physics.SetLinearVelocity(ent, Vector2.Zero);
        _physics.SetAngularVelocity(ent, 0f);
        ent.Comp.AttachedEntity = attachedEnt;
        Dirty(ent);
        RemComp<ItemComponent>(ent);
        RemComp<PullableComponent>(ent);
    }
}
