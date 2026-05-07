// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Trauma.Common.CCVar;
using Content.Trauma.Shared.Heretic.Components.Side;
using Content.Trauma.Shared.Heretic.Systems.Side;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Trauma.Client.Heretic.Systems;

public sealed class FearSystem : SharedFearSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    private FearOverlay _overlay = default!;

    private float _volume = 1f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FearComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<FearComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<FearComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<FearComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        Subs.CVar(_cfg, TraumaCVars.SpecialAudioVolume, SetVolume);

        _overlay = new();
    }

    private void SetVolume(float volume)
    {
        _volume = volume;

        if (!TryComp(_player.LocalEntity, out FearComponent? fear))
            return;

        ResetSounds(fear);
    }

    protected override void UpdateSoundVolume(Entity<FearComponent> ent)
    {
        if (_player.LocalEntity != ent.Owner || ent.Comp.LifeStage > ComponentLifeStage.Running)
        {
            StopSounds(ent.Comp);
            return;
        }

        ResetSounds(ent.Comp);
    }

    private void ResetSounds(FearComponent fear)
    {
        var totalFear = fear.TotalFear;

        var fearVolume = GetSoundVolume(fear.FearVolumeCurve, totalFear);
        fear.FearAudio = PlaySound(fear.FearAudio, fear.FearSound, fearVolume);
        var horrorVolume = GetSoundVolume(fear.HorrorVolumeCurve, totalFear);
        fear.HorrorAudio = PlaySound(fear.HorrorAudio, fear.HorrorSound, horrorVolume);
    }

    private void OnPlayerAttached(Entity<FearComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(Entity<FearComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        StopSounds(ent.Comp);

        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnStartup(Entity<FearComponent> ent, ref ComponentStartup args)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        _overlayMan.AddOverlay(_overlay);
    }

    private void OnShutdown(Entity<FearComponent> ent, ref ComponentShutdown args)
    {
        StopSounds(ent.Comp);

        if (_player.LocalEntity != ent.Owner)
            return;

        _overlayMan.RemoveOverlay(_overlay);
    }

    private float? GetSoundVolume(List<Vector2> volumeCurve, float fear)
    {
        var min = volumeCurve.FindLast(x => x.X <= fear);
        if (min == default)
            return null;
        var max = volumeCurve.Find(x => x.X >= fear);
        if (max == default)
            return null;

        return MathHelper.Lerp(min.Y, max.Y, InverseLerp(min.X, max.X, fear));
    }

    private Entity<AudioComponent>? PlaySound(Entity<AudioComponent>? audio, SoundSpecifier sound, float? volume)
    {
        volume += SharedAudioSystem.GainToVolume(_volume);
        if (volume is not { } vol)
        {
            if (audio is { } aud && !TerminatingOrDeleted(aud))
                _audio.Stop(aud, aud.Comp);
            return null;
        }
        else if (audio is { } aud && !TerminatingOrDeleted(aud))
        {
            _audio.SetVolume(aud, vol, aud.Comp);
            return aud;
        }

        return _audio.PlayGlobal(sound, Filter.Local(), false, AudioParams.Default.WithLoop(true).WithVolume(vol));
    }

    private float InverseLerp(float min, float max, float value)
    {
        return max <= min ? 1f : Math.Clamp((value - min) / (max - min), 0f, 1f);
    }

    // If FearComponent is deleted serverside without predicting deletion clientside, sounds keep playing for some
    // reason. This shouldn't really happen unless you delete the component via VV. At least detaching playing stops
    // sounds normally (I spent too much time trying to fix it)
    private void StopSounds(FearComponent fear)
    {
        if (fear.FearAudio is { } aud1 && !TerminatingOrDeleted(aud1))
            _audio.Stop(aud1, aud1.Comp);
        if (fear.HorrorAudio is { } aud2 && !TerminatingOrDeleted(aud2))
            _audio.Stop(aud2, aud2.Comp);
    }
}
