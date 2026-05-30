// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusEffectNew;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.StatusEffects;

public sealed partial class MusicStatusEffectSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MusicStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<MusicStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);
    }

    private void OnApplied(Entity<MusicStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (_net.IsServer && _audio.PlayPvs(ent.Comp.Sound, ent)?.Entity is { } audio)
        {
            ent.Comp.SoundEntity = audio;
        }
    }

    private void OnRemoved(Entity<MusicStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _audio.Stop(ent.Comp.SoundEntity);
        ent.Comp.SoundEntity = null;
    }
}
