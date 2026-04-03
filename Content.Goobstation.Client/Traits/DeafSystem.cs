// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.Audio;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Configuration;
using Content.Goobstation.Common.CCVar;
using Content.Goobstation.Common.Traits;
using Content.Shared.CCVar;

namespace Content.Goobstation.Client.Traits;

public sealed class DeafnessSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IAudioManager _audio = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private float _originalVolume;
    private bool _deaf;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeafComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<DeafComponent, ComponentShutdown>(OnDeafShutdown);
        SubscribeLocalEvent<DeafComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<DeafComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnComponentStartup(EntityUid uid, DeafComponent component, ComponentStartup args)
    {
        if (_player.LocalEntity == uid)
            TryDeafen();
    }

    private void OnDeafShutdown(EntityUid uid, DeafComponent component, ComponentShutdown args)
    {
        if (_player.LocalEntity == uid)
            TryUndeafen();
    }

    private void OnPlayerAttached(EntityUid uid, DeafComponent component, LocalPlayerAttachedEvent args)
    {
        TryDeafen();
    }

    private void OnPlayerDetached(EntityUid uid, DeafComponent component, LocalPlayerDetachedEvent args)
    {
        TryUndeafen();
    }

    private void TryDeafen()
    {
        if (_deaf)
            return; // don't set _originalVolume to 0 and thus cause gain to be locked at 0

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
