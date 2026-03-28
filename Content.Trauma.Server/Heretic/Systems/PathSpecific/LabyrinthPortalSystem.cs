// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Mind;
using Content.Shared.Physics;
using Content.Shared.Random.Helpers;
using Content.Trauma.Server.Heretic.Components.PathSpecific;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Events;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Heretic.Systems.PathSpecific;

public sealed class LabyrinthPortalSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly EntityLookupSystem _look = default!;

    private TimeSpan _nextSpawn;
    private readonly TimeSpan _spawnDelay = TimeSpan.FromSeconds(1);

    private readonly HashSet<Entity<PhysicsComponent>> _lookupPhysics = new();

    private const int CollisionMask = (int) (CollisionGroup.Impassable | CollisionGroup.HighImpassable |
                                             CollisionGroup.LowImpassable | CollisionGroup.MidImpassable);

    private EntityQuery<MindComponent> _mindQuery;
    private EntityQuery<HereticComponent> _hereticQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LabyrinthPortalComponent, HereticStateChangedEvent>(OnStateChanged);
        SubscribeLocalEvent<LabyrinthPortalComponent, SetGhoulBoundHereticEvent>(OnBoundHeretic);

        _mindQuery = GetEntityQuery<MindComponent>();
        _hereticQuery = GetEntityQuery<HereticComponent>();
    }

    private void OnBoundHeretic(Entity<LabyrinthPortalComponent> ent, ref SetGhoulBoundHereticEvent args)
    {
        ent.Comp.HereticMind = args.HereticMind;
    }

    private void OnStateChanged(Entity<LabyrinthPortalComponent> ent, ref HereticStateChangedEvent args)
    {
        ent.Comp.Paused = args.IsDead;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        if (now < _nextSpawn)
            return;

        _nextSpawn = now + _spawnDelay;

        var queue = EntityQueryEnumerator<LabyrinthPortalComponent, TransformComponent>();
        while (queue.MoveNext(out _, out var portal, out var xform))
        {
            if (portal.Paused)
                continue;

            if (!_random.Prob(portal.SpawnChance))
                continue;

            portal.SpawnedMobs = portal.SpawnedMobs.Where(Exists).ToList();

            if (portal.SpawnedMobs.Count >= portal.MaxMobs)
                continue;

            portal.SpawnChance = MathF.Max(portal.MinSpawnChance, portal.SpawnChance - portal.ChanceReduction);

            _lookupPhysics.Clear();
            _look.GetEntitiesInRange(xform.Coordinates, 1.5f, _lookupPhysics, LookupFlags.Static);
            foreach (var ent in _lookupPhysics)
            {
                if (!ent.Comp.Hard)
                    continue;

                if ((ent.Comp.CollisionLayer & CollisionMask) == 0)
                    continue;

                QueueDel(ent);
            }

            var table = _proto.Index(portal.ToSpawn);
            var mob = table.Pick(_random);
            var spawned = Spawn(mob, xform.Coordinates);
            portal.SpawnedMobs.Add(spawned);

            if (!Exists(portal.HereticMind) || !_hereticQuery.TryComp(portal.HereticMind.Value, out var heretic) ||
                !_mindQuery.TryComp(portal.HereticMind.Value, out var mind) ||
                mind.OwnedEntity is not { } body)
                continue;

            heretic.Minions.Add(spawned);
            var ev = new SetGhoulBoundHereticEvent(body, portal.HereticMind.Value, null);
            RaiseLocalEvent(spawned, ref ev);
        }
    }
}
