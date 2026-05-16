// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.StatusEffects;

/// <summary>
/// Status effect that plays a sound when the status effect gets applied, and removes it once the status effect gets removed.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MusicStatusEffectComponent : Component
{
    /// <summary>
    /// The sound to play.
    /// </summary>
    [DataField(required: true)]
    public SoundSpecifier Sound;

    /// <summary>
    /// The sound entity, used to stop it from playing once the status effect gets removed.
    /// </summary>
    [DataField]
    public EntityUid? SoundEntity;
}
