// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Trauma.Common.Audio;
using Content.Trauma.Common.CCVar;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;

namespace Content.Trauma.Client.Audio;

public sealed class CopyrightedAudioSystem : EntitySystem
{
// entire thing is disabled on debug because its evil and debug asserts
#if DEBUG
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private EntityQuery<AudioComponent> _query;

    private bool _streamerMode;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<AudioComponent>();

        SubscribeLocalEvent<CopyrightedAudioComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CopyrightedAudioComponent, ComponentShutdown>(OnShutdown);
        _cfg.OnValueChanged(TraumaCVars.StreamerMode, x => { _streamerMode = x; UpdateSounds(); }, true);
        //Subs.CVar(_cfg, TraumaCVars.StreamerMode, x => { _streamerMode = x; UpdateSounds(); }, true);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        // TODO: this is fucking evil, but theres no way to set audio data without server overriding it
        UpdateSounds();
    }

    private void OnInit(Entity<CopyrightedAudioComponent> ent, ref ComponentInit args)
    {
        Update(ent.Owner);
    }

    private void OnShutdown(Entity<CopyrightedAudioComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent))
            Update(ent.Owner);
    }

    /// <summary>
    /// Updates all existing copyrighted sounds for the current streamer mode setting.
    /// </summary>
    public void UpdateSounds()
    {
        var query = AllEntityQuery<CopyrightedAudioComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            Update(uid);
        }
    }

    public void Update(EntityUid uid)
    {
        var state = _streamerMode ? AudioState.Paused : AudioState.Playing;
        var audio = _query.Comp(uid);
        _audio.SetState(uid, state, component: audio);
        audio.NetSyncEnabled = _streamerMode; // prevent server trolling it
    }
#endif
}
