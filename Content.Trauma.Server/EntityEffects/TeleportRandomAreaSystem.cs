// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.EntityEffects;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Trauma.Shared.Areas;
using Content.Trauma.Shared.EntityEffects;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Trauma.Server.EntityEffects;

public sealed class TeleportRandomAreaSystem : EntityEffectSystem<TransformComponent, TeleportRandomArea>
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    public const int Oxygen = (int) Gas.Oxygen;

    private List<EntityCoordinates> _areas = new();

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<TeleportRandomArea> args)
    {
        // TODO: make a open areas cache somewhere.....
        var mask = CollisionGroup.MobMask;
        _areas.Clear();
        var query = EntityQueryEnumerator<AreaComponent, TransformComponent>();
        var map = ent.Comp.MapID;
        var safe = args.Effect.Safe;
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapID != map)
                continue;

            // TODO: add area teleport blacklist here if its needed for anything in the future

            var coords = xform.Coordinates;
            if (_turf.GetTileRef(coords) is not {} tile || _turf.IsTileBlocked(tile, mask))
                continue;

            if (safe && IsTileUnsafe((uid, xform)))
                continue;

            _areas.Add(coords);
        }

        if (_areas.Count == 0)
            return;

        var area = _random.PickAndTake(_areas);
        // TODO: backport TeleportationSystem and use it with poof effects
        _transform.SetCoordinates(ent.Owner, area);
    }

    private bool IsTileUnsafe(Entity<TransformComponent> area)
        => _atmos.GetTileMixture(area) is not {} mixture || // space
            mixture.Temperature <= 270 || mixture.Temperature >= 360 || // bad temp
            mixture.Pressure <= 20 || mixture.Pressure >= 300 || // bad pressure
            mixture[Oxygen] < 16; // not enough oxygen
}
