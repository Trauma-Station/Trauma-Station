// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.CCVar;
using Content.Server.Decals;
using Content.Server.Gravity;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Decals;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Content.Shared.Standing;
using Content.Trauma.Common.Footprints;
using Content.Trauma.Server.Decals;
using Content.Trauma.Shared.Footprints;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Configuration;

namespace Content.Trauma.Server.Footprints;

public sealed class FootprintSystem : EntitySystem
{
    [Dependency] private readonly DecalSystem _decal = default!;
    [Dependency] private readonly DecalDespawnSystem _despawn = default!;
    [Dependency] private readonly GravitySystem _gravity = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly EntityQuery<MapGridComponent> _gridQuery = default!;
    [Dependency] private readonly EntityQuery<NoFootprintsComponent> _noFootprintsQuery = default!;
    [Dependency] private readonly EntityQuery<PuddleComponent> _puddleQuery = default!;

    public static readonly ProtoId<DecalPrototype> Footprint = "Footprint";
    public static readonly ProtoId<DecalPrototype> BodySmear = "BodySmear";

    public const float MaxAlpha = 0.7f; // base of the exponential alpha curve
    public const int MaxStepsStuck = 5; // max footprints you can leave without walking over another puddle
    public const int MaxDecals = 2; // don't add footprints if there are this many decals near you

    private float _minimumPuddleSize;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FootprintOwnerComponent, MoveEvent>(OnMove);

        Subs.CVar(_cfg, GoobCVars.MinimumPuddleSizeForFootprints, value => _minimumPuddleSize = value, true);
    }

    private void OnMove(Entity<FootprintOwnerComponent> ent, ref MoveEvent e)
    {
        if (_noFootprintsQuery.HasComp(ent))
            return;

        if (_gravity.IsWeightless(ent.Owner) || !e.OldPosition.IsValid(EntityManager) || !e.NewPosition.IsValid(EntityManager))
            return;

        var oldPosition = _transform.ToMapCoordinates(e.OldPosition).Position;
        var newPosition = _transform.ToMapCoordinates(e.NewPosition).Position;

        ent.Comp.Distance += Vector2.Distance(newPosition, oldPosition);

        var standing = !_standing.IsDown(ent.Owner);

        var requiredDistance = standing ? ent.Comp.FootDistance : ent.Comp.BodyDistance;

        if (ent.Comp.Distance < requiredDistance)
            return;

        ent.Comp.Distance -= requiredDistance;

        var attemptEv = new FootprintLeaveAttemptEvent();
        RaiseLocalEvent(ent, ref attemptEv);
        if (attemptEv.Cancelled)
            return;

        var xform = Transform(ent);
        if (xform.GridUid is not {} gridUid)
            return;

        if (!_gridQuery.TryComp(gridUid, out var grid))
            return;

        EntityCoordinates coordinates = new(ent, standing ? ent.Comp.NextFootOffset : 0, 0);

        ent.Comp.NextFootOffset = -ent.Comp.NextFootOffset;

        var tile = _map.CoordinatesToTile(gridUid, grid, coordinates);

        if (TrySoakPuddle(ent, (gridUid, grid), tile))
            return; // stop here, don't leave footprints ontop of puddles

        Angle rotation;

        if (!standing)
        {
            // TODO: Surely theres a helper for this
            var oldLocalPosition = _map.WorldToLocal(gridUid, grid, oldPosition);
            var newLocalPosition = _map.WorldToLocal(gridUid, grid, newPosition);

            rotation = (newLocalPosition - oldLocalPosition).ToAngle();
        }
        else
            rotation = xform.LocalRotation;

        var pos = _transform.WithEntityId(coordinates, gridUid).Position;
        LeaveFootprints(ent, gridUid, pos, rotation, standing);
    }

    private bool TrySoakPuddle(Entity<FootprintOwnerComponent> ent, Entity<MapGridComponent> grid, Vector2i tile)
    {
        if (GetPuddle(grid, tile) is not {} puddle)
            return false;

        if (!_solution.TryGetSolution(puddle.Owner, puddle.Comp.SolutionName, out _, out var sol))
            return false;

        // only make footprints if a puddle contains enough of a reagent that can form footprints
        if (sol.Volume < _minimumPuddleSize)
            return false;

        // just replace the old color it's not really important
        ent.Comp.Color = sol.GetColor(_proto);
        // and add some steps, 1 footstep per u, capped
        ent.Comp.Steps += sol.Volume.Int();
        ent.Comp.Steps = Math.Min(ent.Comp.Steps, MaxStepsStuck);

        return true;
    }

    private void LeaveFootprints(Entity<FootprintOwnerComponent> ent, EntityUid grid, Vector2 pos, Angle rot, bool standing)
    {
        if (ent.Comp.Steps <= 0)
            return;

        if (_decal.GetDecalsInRange(grid, pos).Count > MaxDecals)
            return; // too many nearby

        // minimum power of 1 so its never 100% opaque.
        var step = ent.Comp.Steps - 1;
        var power = Math.Max(MaxStepsStuck - step, 1);
        var alpha = MathF.Pow(MaxAlpha, power);
        var color = ent.Comp.Color.WithAlpha(alpha);
        var id = standing ? Footprint : BodySmear;

        var coords = new EntityCoordinates(grid, pos - new Vector2(0.5f, 0.5f));
        if (!_decal.TryAddDecal(id, coords, out var decal, color, rot, zIndex: 1, cleanable: true))
            return; // failed to add it somehow...

        _despawn.QueueDespawn(decal);

        // consume the step, it got placed
        ent.Comp.Steps = step;
    }

    private Entity<PuddleComponent>? GetPuddle(Entity<MapGridComponent> grid, Vector2i pos)
    {
        var anchored = _map.GetAnchoredEntitiesEnumerator(grid, grid, pos);
        while (anchored.MoveNext(out var uid))
        {
            if (_puddleQuery.TryComp(uid, out var comp))
                return (uid.Value, comp);
        }

        return null;
    }
}
