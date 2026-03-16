// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2025 username <113782077+whateverusername0@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 whateverusername0 <whateveremail>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Heretic.Components.PathSpecific;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.Abilities;

public sealed class HereticFlamesSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var eqe = EntityQueryEnumerator<HereticFlamesComponent>();
        while (eqe.MoveNext(out var uid, out var hfc))
        {
            hfc.LifetimeTimer += frameTime;
            if (hfc.LifetimeTimer >= hfc.LifetimeDuration)
            {
                RemCompDeferred(uid, hfc);
                continue;
            }

            hfc.UpdateTimer -= frameTime;
            if (hfc.UpdateTimer > 0f)
                continue;

            hfc.UpdateTimer = hfc.UpdateDuration;
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
