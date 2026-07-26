// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Weather;
using Content.Shared.Whitelist;
using Content.Trauma.Shared.Areas;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Weather;

/// <summary>
/// Handles weather effects for exposed mobs.
/// </summary>
public sealed partial class WeatherEffectsSystem : EntitySystem
{
    [Dependency] private AreaSystem _area = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedWeatherSystem _weather = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;
    [Dependency] private EntityQuery<MobStateComponent> _mobQuery = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<WeatherEffectsComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextUpdate)
                continue;

            comp.NextUpdate = now + comp.UpdateDelay;
            if (_net.IsServer) // dont dirty clientside so it doesnt reset state, server can sync itself if needed
                Dirty(uid, comp);

            if (Transform(uid).MapUid is not {} map)
                continue;

            // client only predicts effects for itself
            if (_player.LocalEntity is {} player)
                UpdateEffects(map, player, comp);
            else if (_net.IsServer)
                UpdateAllEffects(map, comp);
        }
    }

    private void UpdateAllEffects(EntityUid map, WeatherEffectsComponent weather)
    {
        var query = EntityQueryEnumerator<MobStateComponent>();
        while (query.MoveNext(out var uid, out var mob))
        {
            UpdateEffects(map, uid, mob, weather);
        }
    }

    private void UpdateEffects(EntityUid map, EntityUid uid, WeatherEffectsComponent weather)
    {
        if (!_mobQuery.TryComp(uid, out var mob))
            return;

        UpdateEffects(map, uid, mob, weather);
    }

    private void UpdateEffects(EntityUid map, EntityUid uid, MobStateComponent mob, WeatherEffectsComponent weather)
    {
        // don't give dead bodies 10000 burn, that's not fun for anyone
        if (mob.CurrentState == MobState.Dead)
            return;

        var xform = Transform(uid);
        if (xform.MapUid != map)
            return;

        if (xform.GridUid is {} gridUid)
        {
            // if any safe areas are defined, check them against the mob's area
            if (weather.SafeAreas.Count > 0 &&
                _area.GetArea(gridUid, xform.Coordinates) is {} area &&
                Prototype(area)?.ID is {} areaId &&
                weather.SafeAreas.Contains(areaId))
            {
                return;
            }

            // if not in space, check for being indoors
            if (weather.SafeIndoors && _gridQuery.TryComp(gridUid, out var grid))
            {
                var tile = _map.GetTileRef((gridUid, grid), xform.Coordinates);
                if (!_weather.CanWeatherAffect((gridUid, grid, null), tile))
                    return;
            }
        }

        _effects.ApplyEffects(uid, weather.Effects);
    }
}
