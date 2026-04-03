// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Audio;
using Robust.Shared.Player;

namespace Content.Trauma.Client.Audio;

/// <summary>
/// Some engine shitcode rarely sets your map to an invalid entity which causes error spam for ambienve.
/// This prevents that happening i hope.
/// </summary>
public sealed class EvilAudioSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayAmbientMusicEvent>(OnPlayAmbientMusic);
    }

    private void OnPlayAmbientMusic(ref PlayAmbientMusicEvent args)
    {
        if (args.Cancelled || _player.LocalEntity is not {} player)
            return;

        args.Cancelled = Transform(player).MapUid == null;
    }
}
