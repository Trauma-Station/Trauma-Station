// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Systems;
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
/// Handles weather damage for exposed mobs.
/// </summary>
public sealed partial class WeatherDamageSystem : EntitySystem
{
    [Dependency] private AreaSystem _area = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
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
        var query = EntityQueryEnumerator<WeatherDamageComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextUpdate)
                continue;

            comp.NextUpdate = now + comp.UpdateDelay;
            Dirty(uid, comp);

            if (Transform(uid).MapUid is not {} map)
            {
                Log.Error($"Ash storm {ToPrettyString(uid)} happened in nullspace!");
                continue;
            }

            // client only predicts damage for itself
            if (_player.LocalEntity is {} player)
                UpdateDamage(map, player, comp);
            else if (_net.IsServer)
                UpdateAllDamage(map, comp);
        }
    }

    private void UpdateAllDamage(EntityUid map, WeatherDamageComponent weather)
    {
        var query = EntityQueryEnumerator<MobStateComponent>();
        while (query.MoveNext(out var uid, out var mob))
        {
            UpdateDamage(map, uid, mob, weather);
        }
    }

    private void UpdateDamage(EntityUid map, EntityUid uid, WeatherDamageComponent weather)
    {
        if (!_mobQuery.TryComp(uid, out var mob))
            return;

        UpdateDamage(map, uid, mob, weather);
    }

    private void UpdateDamage(EntityUid map, EntityUid uid, MobStateComponent mob, WeatherDamageComponent weather)
    {
        // don't give dead bodies 10000 burn, that's not fun for anyone
        if (mob.CurrentState == MobState.Dead)
            return;

        var xform = Transform(uid);
        if (xform.MapUid != map ||
            _whitelist.IsWhitelistPass(weather.Blacklist, uid))
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

        _damageable.ChangeDamage(uid, weather.Damage, interruptsDoAfters: false);
    }
}
