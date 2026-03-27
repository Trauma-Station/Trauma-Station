// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Trauma.Server.Heretic.Components.PathSpecific;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Heretic.Systems.PathSpecific;

public sealed class HereticFlamesSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticFlamesComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<HereticFlamesComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.LifetimeTimer = _timing.CurTime + ent.Comp.LifetimeDuration;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var eqe = EntityQueryEnumerator<HereticFlamesComponent>();
        while (eqe.MoveNext(out var uid, out var hfc))
        {
            if (hfc.LifetimeTimer < now)
            {
                RemCompDeferred(uid, hfc);
                continue;
            }

            if (hfc.UpdateTimer > now)
                continue;

            hfc.UpdateTimer = now + hfc.UpdateDuration;
            SpawnFireBox(uid, hfc.FireProto, hfc.Range, false);
            hfc.Range += hfc.RangeIncrease;
        }
    }

    public void SpawnFireBox(EntityUid relative, EntProtoId proto, int range = 0, bool hollow = true)
    {
        if (range == 0)
        {
            Spawn(proto, Transform(relative).Coordinates);
            return;
        }

        var xform = Transform(relative);

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var gridEnt = (xform.GridUid.Value, grid);

        // get tile position of our entity
        if (!_xform.TryGetGridTilePosition(relative, out var tilePos))
            return;

        // make a box
        var pos = _map.TileCenterToVector(gridEnt, tilePos);
        var confines = new Box2(pos, pos).Enlarged(range);
        var box = _map.GetLocalTilesIntersecting(relative, grid, confines).ToList();

        // hollow it out if necessary
        if (hollow)
        {
            var confinesS = new Box2(pos, pos).Enlarged(Math.Max(range - 1, 0));
            var boxS = _map.GetLocalTilesIntersecting(relative, grid, confinesS).ToList();
            box = box.Where(b => !boxS.Contains(b)).ToList();
        }

        // fill the box
        foreach (var tile in box)
        {
            Spawn(proto, _map.GridTileToWorld((EntityUid) xform.GridUid, grid, tile.GridIndices));
        }
    }

}
