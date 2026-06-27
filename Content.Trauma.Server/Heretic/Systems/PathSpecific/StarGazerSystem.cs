// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Ghost;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Popups;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.Ghoul;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Cosmos;
using Content.Trauma.Shared.Teleportation;
using Content.Trauma.Shared.Wizard.FadingTimedDespawn;
using Robust.Server.GameStates;
using Robust.Shared.Player;

namespace Content.Trauma.Server.Heretic.Systems.PathSpecific;

public sealed partial class StarGazerSystem : SharedStarGazerSystem
{
    [Dependency] private PvsOverrideSystem _pvs = default!;
    [Dependency] private GhostRoleSystem _ghostRole = default!;
    [Dependency] private TeleportSystem _teleport = default!;
    [Dependency] private PopupSystem _popup = default!;

    [Dependency] private EntityQuery<GhostRoleComponent> _ghostRoleQuery = default!;
    [Dependency] private EntityQuery<ActorComponent> _actorQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LaserBeamEndpointComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<LaserBeamEndpointComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<CosmosPassiveComponent, ResetStarGazerConsciousnessEvent>(OnReset);

        SubscribeLocalEvent<StarGazerComponent, HereticStateChangedEvent>(OnStateChanged);
        SubscribeLocalEvent<StarGazerComponent, StarGazerSeekMasterEvent>(OnSeekMaster);
        SubscribeLocalEvent<StarGazerComponent, TakeGhostRoleEvent>(OnTakeGhostRole,
            after: [typeof(GhostRoleSystem)]);

        SubscribeLocalEvent<GhostAttemptHandleEvent>(OnGhost);
    }

    private void OnGhost(GhostAttemptHandleEvent args)
    {
        if (HasComp<StarGazerComponent>(args.Mind.CurrentEntity))
            args.CanReturnGlobal = false;
    }

    private void OnSeekMaster(Entity<StarGazerComponent> ent, ref StarGazerSeekMasterEvent args)
    {
        if (!TryComp(ent, out HereticMinionComponent? minion) ||
            !Exists(minion.BoundHeretic))
            return;


        args.Handled = TeleportStarGazer(ent, minion.BoundHeretic.Value);
    }

    private bool TeleportStarGazer(Entity<StarGazerComponent> ent, EntityUid target)
    {
        if (IsPaused(target))
            return false;

        var oldCoords = Transform(ent).Coordinates;
        var newCoords = Transform(target).Coordinates;
        if (!_teleport.Teleport(ent.Owner, newCoords, ent.Comp.TeleportSound, user: ent, predicted: false))
            return false;

        Spawn(ent.Comp.TeleportEffect, oldCoords);
        Spawn(ent.Comp.TeleportEffect, newCoords);
        return true;
    }

    private void OnStateChanged(Entity<StarGazerComponent> ent, ref HereticStateChangedEvent args)
    {
        if (!args.IsDead)
        {
            RemCompDeferred<FadingTimedDespawnComponent>(ent);
            Status.TryRemoveStatusEffect(ent, ent.Comp.InactiveStatus);
            return;
        }

        if (args.Temporary)
        {
            if (!Status.HasStatusEffect(ent, ent.Comp.InactiveStatus))
                Status.TryAddStatusEffect(ent, ent.Comp.InactiveStatus, out _);
            return;
        }

        KillStarGazer(ent);
    }

    private void KillStarGazer(EntityUid starGazer)
    {
        var fading = EnsureComp<FadingTimedDespawnComponent>(starGazer);
        fading.FadeOutTime = 5f;
        fading.Lifetime = 0f;
    }

    private void OnTakeGhostRole(Entity<StarGazerComponent> ent, ref TakeGhostRoleEvent args)
    {
        if (!args.TookRole || ent.Comp.ResettingMindSession == null)
            return;

        _popup.PopupCoordinates(Loc.GetString("heretic-stargazer-consciousness-reset-target"),
            Transform(ent).Coordinates,
            ent.Comp.ResettingMindSession,
            PopupType.LargeCaution);

        ent.Comp.ResettingMindSession = null;

        if (!TryComp(ent, out HereticMinionComponent? minion) || minion.BoundHeretic is not { } heretic)
            return;

        _popup.PopupEntity(Loc.GetString("heretic-stargazer-consciousness-reset-user"),
            heretic,
            heretic,
            PopupType.Large);
    }

    private void OnReset(Entity<CosmosPassiveComponent> ent, ref ResetStarGazerConsciousnessEvent args)
    {
        args.Handled = true;

        if (ResolveStarGazer(ent.Owner, out var spawned) is not { } starGazer || spawned)
            return;

        if (TryComp(starGazer, out ActorComponent? actor))
            starGazer.Comp.ResettingMindSession = actor.PlayerSession;

        EnsureComp<GhostTakeoverAvailableComponent>(starGazer).IgnoreMindCheck = true;
        var role = EnsureComp<GhostRoleComponent>(starGazer);
        _ghostRole.SetTaken(role, false);
        _ghostRole.RegisterGhostRole((starGazer, role));

        starGazer.Comp.GhostRoleTimer = Timing.CurTime + starGazer.Comp.GhostRoleTime;
    }

    private void RemoveGhostRole(Entity<StarGazerComponent, GhostRoleComponent?> ent, bool hasMind, bool resettingMind)
    {
        ent.Comp1.ResettingMindSession = null;

        if (!hasMind || resettingMind || !Resolve(ent, ref ent.Comp2, false) || ent.Comp2.Taken)
            return;

        _ghostRole.SetTaken(ent.Comp2, true);
        _ghostRole.UnregisterGhostRole((ent.Owner, ent.Comp2));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;

        var query = EntityQueryEnumerator<StarGazerComponent, HereticMinionComponent, MindContainerComponent,
            TransformComponent>();
        while (query.MoveNext(out var uid, out var starGazer, out var minion, out var mindContainer, out var xform))
        {
            var hasMind = mindContainer.HasMind;
            var resettingMind = starGazer.ResettingMindSession != null;
            var changedSession = resettingMind && (!_actorQuery.TryComp(uid, out var actor) ||
                                                   actor.PlayerSession != starGazer.ResettingMindSession);

            if (changedSession)
                RemoveGhostRole((uid, starGazer), hasMind, resettingMind);
            else if (hasMind && resettingMind && _ghostRoleQuery.TryComp(uid, out var ghostRole))
            {
                if (now > starGazer.GhostRoleTimer)
                {
                    starGazer.GhostRoleTimer = now + starGazer.GhostRoleTime;

                    RemoveGhostRole((uid, starGazer, ghostRole), hasMind, resettingMind);

                    if (!Exists(minion.BoundHeretic))
                        continue;

                    _popup.PopupEntity(Loc.GetString("heretic-stargazer-consciousness-reset-fail"),
                        minion.BoundHeretic.Value,
                        minion.BoundHeretic.Value,
                        PopupType.Large);
                }
            }
            else
                RemoveGhostRole((uid, starGazer), hasMind, resettingMind);

            if (now < starGazer.ResetDistanceTimer)
                continue;

            starGazer.ResetDistanceTimer = now + starGazer.ResetDistanceTime;

            if (!Exists(minion.BoundHeretic))
                continue;

            if (!Xform.InRange((uid, xform), minion.BoundHeretic.Value, starGazer.MaxDistance))
                TeleportStarGazer((uid, starGazer), minion.BoundHeretic.Value);
        }
    }

    private void OnShutdown(Entity<LaserBeamEndpointComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.PvsOverride)
            _pvs.RemoveGlobalOverride(ent);
    }

    private void OnStartup(Entity<LaserBeamEndpointComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.PvsOverride)
            _pvs.AddGlobalOverride(ent);
    }

    protected override void OnStarGazeStartup(Entity<StarGazeComponent> ent, ref ComponentStartup args)
    {
        base.OnStarGazeStartup(ent, ref args);

        _pvs.AddGlobalOverride(ent);
    }

    protected override void OnStarGazeShutdown(Entity<StarGazeComponent> ent, ref ComponentShutdown args)
    {
        base.OnStarGazeShutdown(ent, ref args);

        _pvs.RemoveGlobalOverride(ent);
    }
}
