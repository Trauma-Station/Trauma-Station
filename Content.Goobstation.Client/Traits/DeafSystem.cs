// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.CCVar;
using Content.Goobstation.Common.Traits;
using Content.Shared.CCVar;
using Robust.Client.Audio;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Configuration;

namespace Content.Goobstation.Client.Traits;

public sealed partial class DeafnessSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IAudioManager _audio = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    private float _originalVolume;
    private bool _deaf;

    [SubscribeLocalEvent]
    private void OnComponentStartup(Entity<DeafComponent> ent, ref ComponentStartup args)
    {
        if (_player.LocalEntity == ent.Owner)
            TryDeafen();
    }

    [SubscribeLocalEvent]
    private void OnDeafShutdown(Entity<DeafComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity == ent.Owner)
            TryUndeafen();
    }

    [SubscribeLocalEvent]
    private void OnPlayerAttached(Entity<DeafComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        TryDeafen();
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(Entity<DeafComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        TryUndeafen();
    }

    private void TryDeafen()
    {
        if (_deaf)
            return; // don't set _originalVolume to 0 and thus cause gain to be locked at 0

        // TODO: lol lmao properly mute sounds you can just change the slider
        // Save the current volume before muting
        _originalVolume = _cfg.GetCVar(CCVars.AudioMasterVolume);
        _audio.SetMasterGain(0);
        _deaf = true;
    }

    private void TryUndeafen()
    {
        if (!_deaf)
            return;

        _audio.SetMasterGain(_originalVolume);
        _deaf = false;
    }
}
