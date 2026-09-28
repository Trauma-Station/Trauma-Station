// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chat.Systems;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Systems;
using Content.Shared.Random.Helpers;
using Content.Shared.Popups;
using Content.Trauma.Shared.BloodCult.Items;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Trauma.Server.BloodCult.Items;

public sealed partial class ServerShuttleCurseSystem : ShuttleCurseSystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private EmergencyShuttleSystem _evac = default!;
    [Dependency] private RoundEndSystem _roundEnd = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    protected override void DelayShuttle(Entity<ShuttleCurseComponent> ent, Entity<ShuttleCurseProviderComponent> provider, EntityUid user)
    {
        if (_evac.EmergencyShuttleArrived)
        {
            Popup.PopupEntity(Loc.GetString("shuttle-curse-shuttle-arrived"), ent, user, PopupType.MediumCaution);
            return;
        }

        if (_roundEnd.ExpectedCountdownEnd is null)
        {
            Popup.PopupEntity(Loc.GetString("shuttle-curse-shuttle-not-called"), ent, user, PopupType.MediumCaution);
            return;
        }

        _roundEnd.DelayShuttle(ent.Comp.DelayTime);

        var messages = ProtoMan.Index(ent.Comp.CurseMessages);
        var cursedMessage = string.Concat(Loc.GetString(_random.Pick(messages)),
            " ",
            Loc.GetString("shuttle-curse-success-global", ("time", ent.Comp.DelayTime.TotalMinutes)));

        _chat.DispatchGlobalAnnouncement(cursedMessage,
            Loc.GetString("shuttle-curse-system-failure"),
            colorOverride: Color.Gold);

        Popup.PopupEntity(Loc.GetString("shuttle-curse-success"), user, user, PopupType.Large);
        provider.Comp.CurrentUses++;
        Dirty(provider);

        _audio.PlayPvs(ent.Comp.ScatterSound, Transform(ent).Coordinates);
        Del(ent);
    }
}
