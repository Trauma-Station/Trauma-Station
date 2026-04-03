// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Audio;
using Content.Client.Chat.Managers;
using Content.Shared.Chat;
using Content.Shared.Coordinates.Helpers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.Audio;

/// <summary>
/// Some engine shitcode is broken which leads to deathly lag when it reaches ambience -> areas.
/// This prevents that happening i hope.
/// </summary>
public sealed class EvilAudioSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private TimeSpan _nextWarn;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayAmbientMusicEvent>(OnPlayAmbientMusic);
    }

    private void OnPlayAmbientMusic(ref PlayAmbientMusicEvent args)
    {
        if (args.Cancelled || _player.LocalEntity is not {} player)
            return;

        // mimic the area ambience pipeline of GetArea -> GetAreaCentered -> GetEntitesInRange -> BANG
        var coords = Transform(player).Coordinates;
        var snapped = coords.SnapToGrid(EntityManager, _mapMan);
        var mapPos = _transform.ToMapCoordinates(snapped);
        var map = mapPos.MapId;
        var mapUid = _map.GetMapOrInvalid(map);
        args.Cancelled = !mapUid.IsValid();

        if (!args.Cancelled)
            return;

        var now = _timing.CurTime;
        if (now < _nextWarn)
            return;

        Log.Error("Hell bug detected, please report this with the below information!");
        Log.Error($"player = {ToPrettyString(player)}");
        Log.Error($"coords = {coords} / snapped = {snapped}");
        Log.Error($"mapPos = {mapPos}");
        Log.Error($"map = {map}");
        Log.Error($"mapUid = {mapUid} vs {Transform(player).MapUid}");
        _nextWarn = now + TimeSpan.FromSeconds(60);
        // this'll probably get your attention!
        _chat.SendMessage("HELLO CHUD OPEN YOUR CONSOLE AND REPORT IT THIS BUG", ChatSelectChannel.LOOC);
    }
}
