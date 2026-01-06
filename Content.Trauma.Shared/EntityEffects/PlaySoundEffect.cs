// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

public sealed partial class PlaySoundEffect : EntityEffectBase<PlaySoundEffect>
{
    /// <summary>
    /// The sound to play
    /// </summary>
    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    /// <summary>
    /// Play the sound at the position instead of parented to the target entity.
    /// Useful if the entity is deleted after.
    /// </summary>
    [DataField]
    public bool Positional = true;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null; // idc
}

public sealed class PlaySoundEffectSystem : EntityEffectSystem<TransformComponent, PlaySoundEffect>
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EffectDataSystem _data = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<PlaySoundEffect> args)
    {
        var sound = args.Effect.Sound;
        var user = _data.GetUser(ent); // only predicted for debug effect stick etc where there is a clear user
        if (args.Effect.Positional)
            _audio.PlayPredicted(sound, ent.Comp.Coordinates, user);
        else
            _audio.PlayPredicted(sound, ent, user);
    }
}
