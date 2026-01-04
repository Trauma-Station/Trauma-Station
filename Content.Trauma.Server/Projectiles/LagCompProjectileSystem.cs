// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Server.Movement.Components;
using Content.Server.Movement.Systems;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Trauma.Common.CCVar;
using Content.Trauma.Common.Projectiles;
using Content.Trauma.Shared.Projectiles;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;

namespace Content.Trauma.Server.Projectiles;

/// <summary>
/// Compensates for shooter's lag by using flyby fixture to check for where the shooter saw the target at time of shooting.
/// Uses flyby fixture as for most entities this will overlap with the client's opinion of where the target "currently" is.
/// </summary>
public sealed class LagCompProjectileSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly LagCompensationSystem _lag = default!;
    [Dependency] private readonly PredictedProjectileSystem _projectile = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<ActorComponent> _actorQuery;
    private EntityQuery<LagCompensationComponent> _lagQuery;

    /// <summary>
    /// If a projectile is within this distance of a lag-comp'd position for a target, it counts as a hit.
    /// </summary>
    public float Range = 0.6f;

    public override void Initialize()
    {
        base.Initialize();

        _actorQuery = GetEntityQuery<ActorComponent>();
        _lagQuery = GetEntityQuery<LagCompensationComponent>();

        SubscribeLocalEvent<PlayerShotProjectileEvent>(OnShotProjectile);
        SubscribeLocalEvent<LagCompProjectileComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<LagCompProjectileComponent, EndCollideEvent>(OnEndCollide);

        Subs.CVar(_cfg, TraumaCVars.GunLagCompRange, x => Range = x, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<LagCompProjectileComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Targets.Count == 0)
                continue;

            var pos = _transform.GetMapCoordinates(uid);
            foreach (var target in comp.Targets)
            {
                var lagPos = _transform.ToMapCoordinates(_lag.GetCoordinates(target, comp.ShooterSession));
                if (pos.InRange(lagPos, Range))
                {
                    // it's close to where the client saw target at, do the hit
                    _projectile.DoHit(uid, target);
                    // don't need to do lagcomp logic anymore, bullet is being deleted
                    RemCompDeferred(uid, comp);
                }
            }
        }
    }

    private void OnShotProjectile(ref PlayerShotProjectileEvent args)
    {
        // shooter wasn't player-controlled, don't care
        if (!_actorQuery.TryComp(args.User, out var actor))
            return;

        var session = actor.PlayerSession;

        // add lag comp so it's fairer for high ping chuds
        var comp = EnsureComp<LagCompProjectileComponent>(args.Projectile);
        comp.ShooterSession = session;

        // this lets the client ignore the server-spawned projectile that it predicted shooting
        var ev = new ShotPredictedProjectileEvent()
        {
            Projectile = GetNetEntity(args.Projectile)
        };
        RaiseNetworkEvent(ev, session);
    }

    private void OnStartCollide(Entity<LagCompProjectileComponent> ent, ref StartCollideEvent args)
    {
        if (args.OurEntity != ent.Owner || args.OurFixtureId != SharedFlyBySoundSystem.FlyByFixture)
            return;

        var target = args.OtherEntity;
        if (_lagQuery.HasComp(target))
            ent.Comp.Targets.Add(target);
    }

    private void OnEndCollide(Entity<LagCompProjectileComponent> ent, ref EndCollideEvent args)
    {
        if (args.OurEntity != ent.Owner || args.OurFixtureId != SharedFlyBySoundSystem.FlyByFixture)
            return;

        ent.Comp.Targets.Remove(args.OtherEntity);
    }
}
