// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Content.Shared.Weather;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Weather;

public sealed partial class WeatherSchedulerSystem : EntitySystem
{
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedWeatherSystem _weather = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<WeatherSchedulerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextUpdate)
                continue;

            if (comp.Stage >= comp.Stages.Count)
            {
                if (comp.Temporary)
                {
                    // remove the status effect instead of wrapping
                    QueueDel(uid);
                    continue;
                }
                comp.Stage = 0;
            }

            var stage = comp.Stages[comp.Stage++];
            var duration = TimeSpan.FromSeconds(stage.Duration.Next(_random));
            comp.NextUpdate = now + duration;

            var mapId = Transform(uid).MapID;
            if (stage.Weather is { } weather)
            {
                // crossfade weather smoothly so as one ends the next starts
                if (HasWeather(comp, comp.Stage - 1))
                    duration += SharedWeatherSystem.StartupTime;
                if (HasWeather(comp, comp.Stage + 1))
                    duration += SharedWeatherSystem.ShutdownTime;
                _weather.TryAddWeather(mapId, weather, out _, duration);
            }

            if (stage.Message is { } message)
            {
                var msg = Loc.GetString(message);
                _chat.ChatMessageToManyFiltered(
                    Filter.BroadcastMap(mapId),
                    ChatChannel.Radio,
                    msg,
                    msg,
                    uid,
                    false,
                    true,
                    null);
            }
        }
    }

    private bool HasWeather(WeatherSchedulerComponent comp, int stage)
    {
        if (stage < 0)
            stage = comp.Stages.Count + stage;
        else if (stage >= comp.Stages.Count)
            stage %= comp.Stages.Count;

        return comp.Stages[stage].Weather != null;
    }
}
